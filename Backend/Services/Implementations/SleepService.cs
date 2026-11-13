using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Services.Implementations;

public class SleepService : ISleepService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IAlertNotifier? _notifier;
    private readonly ILogger<SleepService>? _logger;

    public SleepService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IMapper mapper, IAlertNotifier? notifier=null,ILogger<SleepService>? logger=null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _notifier=notifier;_logger=logger;
    }

    public async Task<SleepRecordDto> RecordSleepAsync(
        int astronautId,
        CreateSleepRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Authorization: Monitors cannot write sleep records; Astronauts write own data only
        if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            throw new UnauthorizedAccessException("Flight monitors cannot create sleep records.");
        }

        if (!_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            throw new UnauthorizedAccessException("Only authenticated astronauts can create sleep records.");
        }

        var currentAstronautId = _currentUserService.AstronautId;
        if (currentAstronautId == null)
        {
            throw new UnauthorizedAccessException("Current astronaut identity could not be resolved from token claims.");
        }

        if (astronautId > 0 && astronautId != currentAstronautId.Value)
        {
            throw new UnauthorizedAccessException("Astronauts can only record sleep records for themselves.");
        }

        int effectiveAstronautId = currentAstronautId.Value;

        // 2. Idempotency: verify ClientGeneratedId
        var existingRecord = await _dbContext.SleepRecords
            .AsNoTracking()
            .Include(s => s.Astronaut)
            .FirstOrDefaultAsync(s => s.ClientGeneratedId == request.ClientGeneratedId, cancellationToken);

        if (existingRecord != null)
        {
            if(existingRecord.AstronautId!=effectiveAstronautId)
                throw new UnauthorizedAccessException("The submission ID belongs to a different astronaut.");
            return _mapper.Map<SleepRecordDto>(existingRecord);
        }

        var astronaut = await _dbContext.Astronauts.FindAsync(new object[] { effectiveAstronautId }, cancellationToken);
        if (astronaut == null)
        {
            throw new InvalidOperationException($"Astronaut with ID {effectiveAstronautId} does not exist.");
        }

        if (!astronaut.IsActive)
        {
            throw new InvalidOperationException($"Cannot record sleep for deactivated astronaut {effectiveAstronautId}.");
        }

        // 3. Compute duration and efficiency
        var startUtc = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);
        var recordedAtUtc = DateTime.SpecifyKind(request.RecordedAt, DateTimeKind.Utc);

        var durationHours = Math.Round((endUtc - startUtc).TotalHours, 2);

        double efficiency = request.SleepEfficiencyPercentage;
        if (efficiency <= 0 && (endUtc - startUtc).TotalMinutes > 0)
        {
            var sleepMinutes = request.DeepSleepMinutes + request.RemSleepMinutes + request.LightSleepMinutes;
            efficiency = Math.Round(((double)sleepMinutes / (endUtc - startUtc).TotalMinutes) * 100, 2);
            if (efficiency > 100) efficiency = 100;
        }

        // 4. Build entity
        var record = new SleepRecord
        {
            ClientGeneratedId = request.ClientGeneratedId,
            AstronautId = effectiveAstronautId,
            RecordedAt = recordedAtUtc,
            StartTime = startUtc,
            EndTime = endUtc,
            DurationHours = durationHours,
            Quality = request.Quality,
            DeepSleepMinutes = request.DeepSleepMinutes,
            RemSleepMinutes = request.RemSleepMinutes,
            LightSleepMinutes = request.LightSleepMinutes,
            AwakeMinutes = request.AwakeMinutes,
            SleepEfficiencyPercentage = efficiency,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.SleepRecords.Add(record);
        record.Astronaut=astronaut;
        var alert=await new WellnessRuleService(_dbContext).ApplySleepAsync(record,cancellationToken);
        try {await _dbContext.SaveChangesAsync(cancellationToken);}
        catch(DbUpdateException error) when(SubmissionConflict.IsDuplicate(error,"SleepRecords"))
        {
            _dbContext.ChangeTracker.Clear();
            var duplicate=await _dbContext.SleepRecords.AsNoTracking().Include(s=>s.Astronaut).SingleAsync(s=>s.ClientGeneratedId==request.ClientGeneratedId,cancellationToken);
            if(duplicate.AstronautId!=effectiveAstronautId)throw new UnauthorizedAccessException("The submission ID belongs to a different astronaut.");
            return _mapper.Map<SleepRecordDto>(duplicate);
        }
        if(alert is not null && _notifier is not null)
        {
            try {await _notifier.NotifyAlertCreatedAsync(_mapper.Map<AstronautHealthMonitoring.DTOs.Alerts.HealthAlertDto>(alert),cancellationToken);}
            catch(Exception e){_logger?.LogWarning(e,"Sleep saved; live alert notification failed for astronaut {AstronautId}",effectiveAstronautId);}
        }

        record.Astronaut = astronaut;

        return _mapper.Map<SleepRecordDto>(record);
    }

    public async Task<SleepRecordDto?> GetSleepRecordByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.SleepRecords
            .AsNoTracking()
            .Include(s => s.Astronaut)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (record == null)
        {
            return null;
        }

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || record.AstronautId != myAstronautId.Value)
            {
                return null;
            }
        }

        return _mapper.Map<SleepRecordDto>(record);
    }

    public async Task<PagedResult<SleepRecordDto>> GetSleepHistoryPagedAsync(
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

        var query = _dbContext.SleepRecords
            .AsNoTracking()
            .Include(s => s.Astronaut)
            .Where(s => s.AstronautId == astronautId);

        if (fromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
            query = query.Where(s => s.RecordedAt >= from);
        }

        if (toUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc);
            query = query.Where(s => s.RecordedAt <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.RecordedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SleepRecordDto>
        {
            Items = _mapper.Map<IReadOnlyList<SleepRecordDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<SleepRecordDto>> GetSleepHistoryAsync(
        int astronautId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var paged = await GetSleepHistoryPagedAsync(astronautId, fromUtc, toUtc, 1, 100, cancellationToken);
        return paged.Items;
    }

    public async Task<SleepRecordDto?> GetLatestSleepRecordAsync(int astronautId, CancellationToken cancellationToken = default)
    {
        EnforceAstronautReadScope(astronautId);

        var record = await _dbContext.SleepRecords
            .AsNoTracking()
            .Include(s => s.Astronaut)
            .Where(s => s.AstronautId == astronautId)
            .OrderByDescending(s => s.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return record != null ? _mapper.Map<SleepRecordDto>(record) : null;
    }

    private void EnforceAstronautReadScope(int targetAstronautId)
    {
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || myAstronautId.Value != targetAstronautId)
            {
                throw new UnauthorizedAccessException("Astronauts can only view their own sleep records.");
            }
        }
    }
}

