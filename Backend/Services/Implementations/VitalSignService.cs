using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Services.Implementations;

public class VitalSignService : IVitalSignService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IVitalSignStatusEvaluator _statusEvaluator;
    private readonly IHealthAlertService _healthAlertService;
    private readonly IMapper _mapper;
    private readonly IAlertNotifier? _alertNotifier;
    private readonly ILogger<VitalSignService>? _logger;

    public VitalSignService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IVitalSignStatusEvaluator statusEvaluator,
        IHealthAlertService healthAlertService,
        IMapper mapper,
        IAlertNotifier? alertNotifier = null,
        ILogger<VitalSignService>? logger = null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _statusEvaluator = statusEvaluator;
        _healthAlertService = healthAlertService;
        _mapper = mapper;
        _alertNotifier = alertNotifier;
        _logger = logger;
    }

    public async Task<VitalSignReadingDto> RecordReadingAsync(
        int astronautId,
        CreateVitalSignReadingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Authorization: Monitors cannot create readings; Astronauts can only create for themselves
        if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            throw new UnauthorizedAccessException("Flight monitors cannot create vital sign readings.");
        }

        if (!_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            throw new UnauthorizedAccessException("Only authenticated astronauts can create vital sign readings.");
        }

        var currentAstronautId = _currentUserService.AstronautId;
        if (currentAstronautId == null)
        {
            throw new UnauthorizedAccessException("Current astronaut identity could not be resolved from token claims.");
        }

        if (astronautId > 0 && astronautId != currentAstronautId.Value)
        {
            throw new UnauthorizedAccessException("Astronauts can only record vital signs for themselves.");
        }

        // Server-side enforcement: astronaut ID resolved from verified token claims
        int effectiveAstronautId = currentAstronautId.Value;

        // 2. Idempotency: verify ClientGeneratedId
        var existingReading = await _dbContext.VitalSignReadings
            .AsNoTracking()
            .Include(r => r.Astronaut)
            .Include(r => r.VitalSignType)
            .FirstOrDefaultAsync(r => r.ClientGeneratedId == request.ClientGeneratedId, cancellationToken);

        if (existingReading != null)
        {
            if(existingReading.AstronautId!=effectiveAstronautId)
                throw new UnauthorizedAccessException("The submission ID belongs to a different astronaut.");
            return _mapper.Map<VitalSignReadingDto>(existingReading);
        }

        // 3. Resolve Astronaut and VitalSignType
        var astronaut = await _dbContext.Astronauts.FindAsync(new object[] { effectiveAstronautId }, cancellationToken);
        if (astronaut == null)
        {
            throw new InvalidOperationException($"Astronaut profile with ID {effectiveAstronautId} does not exist.");
        }

        if (!astronaut.IsActive)
        {
            throw new InvalidOperationException($"Cannot record vital signs for deactivated astronaut {effectiveAstronautId}.");
        }

        var vitalSignType = await _dbContext.VitalSignTypes.FindAsync(new object[] { request.VitalSignTypeId }, cancellationToken);
        if (vitalSignType == null)
        {
            throw new InvalidOperationException($"VitalSignType with ID {request.VitalSignTypeId} does not exist.");
        }

        // 4. Resolve Matching Reference Range (prefer MissionApproved, then matching gender, then any active)
        var referenceRanges = await _dbContext.ReferenceRanges
            .Where(r => r.VitalSignTypeId == request.VitalSignTypeId && r.IsActive)
            .ToListAsync(cancellationToken);

        if (referenceRanges.Count == 0)
        {
            throw new InvalidOperationException($"No active reference range configured for VitalSignType ID {request.VitalSignTypeId}.");
        }

        var matchingRange = referenceRanges
            .OrderByDescending(r => r.IsMissionApproved)
            .ThenByDescending(r => r.ApplicableGender == astronaut.Gender)
            .First();

        // 5. Evaluate clinical status via deterministic evaluator
        var status = _statusEvaluator.Evaluate(matchingRange, request.Value, request.SecondaryValue);

        // 6. Build entity with server-controlled timestamps and metadata
        var reading = new VitalSignReading
        {
            ClientGeneratedId = request.ClientGeneratedId,
            AstronautId = effectiveAstronautId,
            VitalSignTypeId = request.VitalSignTypeId,
            Value = request.Value,
            SecondaryValue = request.SecondaryValue,
            Status = status,
            MeasurementMethod = request.MeasurementMethod,
            SensorLocation = request.SensorLocation,
            BodyPosition = request.BodyPosition,
            ActivityContext = request.ActivityContext,
            DataQuality = request.DataQuality,
            MealContext = request.MealContext,
            MinutesSinceLastMeal = request.MinutesSinceLastMeal,
            DeviceId = request.DeviceId,
            Notes = request.Notes,
            RecordedAt = DateTime.SpecifyKind(request.RecordedAt, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        };

        // 7. Atomic transaction: save reading and, if non-routine, create health alert in same transaction
        var isRelational = _dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
        await using var transaction = isRelational
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        HealthAlertDto? createdAlert = null;

        try
        {
            _dbContext.VitalSignReadings.Add(reading);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (status != VitalSignStatus.Routine)
            {
                reading.Astronaut = astronaut;
                reading.VitalSignType = vitalSignType;
                createdAlert = await _healthAlertService.CreateAlertFromReadingAsync(reading, cancellationToken);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch(DbUpdateException error) when(SubmissionConflict.IsDuplicate(error,"VitalSignReadings"))
        {
            if(transaction is not null)await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            var duplicate=await _dbContext.VitalSignReadings.AsNoTracking().Include(r=>r.Astronaut).Include(r=>r.VitalSignType)
                .SingleAsync(r=>r.ClientGeneratedId==request.ClientGeneratedId,cancellationToken);
            if(duplicate.AstronautId!=effectiveAstronautId)throw new UnauthorizedAccessException("The submission ID belongs to a different astronaut.");
            return _mapper.Map<VitalSignReadingDto>(duplicate);
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }

        // 8. Real-time notification: dispatched strictly AFTER transaction commits
        if (createdAlert != null && _alertNotifier != null)
        {
            try
            {
                await _alertNotifier.NotifyAlertCreatedAsync(createdAlert, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Alert notification failed after transaction commit for Alert ID: {AlertId}. Reading save remains valid.", createdAlert.Id);
            }
        }

        // Attach navigations for DTO projection
        reading.Astronaut = astronaut;
        reading.VitalSignType = vitalSignType;

        return _mapper.Map<VitalSignReadingDto>(reading);
    }

    public async Task<VitalSignReadingDto?> GetReadingByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var reading = await _dbContext.VitalSignReadings
            .AsNoTracking()
            .Include(r => r.Astronaut)
            .Include(r => r.VitalSignType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (reading == null)
        {
            return null;
        }

        // Authorization check for astronauts
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || reading.AstronautId != myAstronautId.Value)
            {
                return null;
            }
        }

        return _mapper.Map<VitalSignReadingDto>(reading);
    }

    public async Task<IReadOnlyList<VitalSignReadingDto>> GetLatestReadingsPerMetricAsync(int astronautId, CancellationToken cancellationToken = default)
    {
        EnforceAstronautReadScope(astronautId);

        var readings = await _dbContext.VitalSignReadings
            .LatestPerMetric()
            .AsNoTracking()
            .Include(r => r.Astronaut)
            .Include(r => r.VitalSignType)
            .Where(r => r.AstronautId == astronautId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(cancellationToken);

        var latestPerMetric = readings
            .GroupBy(r => r.VitalSignTypeId)
            .Select(g => g.First())
            .OrderBy(r => r.VitalSignTypeId)
            .ToList();

        return _mapper.Map<IReadOnlyList<VitalSignReadingDto>>(latestPerMetric);
    }

    public async Task<PagedResult<VitalSignReadingDto>> GetHistoryAsync(
        int astronautId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        EnforceAstronautReadScope(astronautId);

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.VitalSignReadings
            .AsNoTracking()
            .Include(r => r.Astronaut)
            .Include(r => r.VitalSignType)
            .Where(r => r.AstronautId == astronautId);

        if (fromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
            query = query.Where(r => r.RecordedAt >= from);
        }

        if (toUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc);
            query = query.Where(r => r.RecordedAt <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.RecordedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<VitalSignReadingDto>
        {
            Items = _mapper.Map<IReadOnlyList<VitalSignReadingDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<VitalSignReadingDto>> GetRecentReadingsAsync(int astronautId, int count = 50, CancellationToken cancellationToken = default)
    {
        EnforceAstronautReadScope(astronautId);

        if (count < 1) count = 50;
        if (count > 200) count = 200;

        var items = await _dbContext.VitalSignReadings
            .AsNoTracking()
            .Include(r => r.Astronaut)
            .Include(r => r.VitalSignType)
            .Where(r => r.AstronautId == astronautId)
            .OrderByDescending(r => r.RecordedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<VitalSignReadingDto>>(items);
    }

    public async Task<IReadOnlyList<VitalSignReadingDto>> GetReadingsByTimeRangeAsync(
        int astronautId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        EnforceAstronautReadScope(astronautId);

        var from = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);

        var items = await _dbContext.VitalSignReadings
            .AsNoTracking()
            .Include(r => r.Astronaut)
            .Include(r => r.VitalSignType)
            .Where(r => r.AstronautId == astronautId && r.RecordedAt >= from && r.RecordedAt <= to)
            .OrderBy(r => r.RecordedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<VitalSignReadingDto>>(items);
    }

    public async Task<IReadOnlyList<VitalSignTypeDto>> GetAllVitalSignTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _dbContext.VitalSignTypes
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<VitalSignTypeDto>>(types);
    }

    public async Task<IReadOnlyList<VitalSignReadingDto>> SyncReadingsAsync(
        int astronautId,
        SyncVitalSignReadingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var results = new List<VitalSignReadingDto>();
        foreach (var readingReq in request.Readings)
        {
            var readingDto = await RecordReadingAsync(astronautId, readingReq, cancellationToken);
            results.Add(readingDto);
        }

        return results;
    }

    private void EnforceAstronautReadScope(int targetAstronautId)
    {
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || myAstronautId.Value != targetAstronautId)
            {
                throw new UnauthorizedAccessException("Astronauts can only view their own vital sign data.");
            }
        }
    }
}

