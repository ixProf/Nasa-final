using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Services.Implementations;

public class MoodService : IMoodService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IAlertNotifier? _notifier;
    private readonly ILogger<MoodService>? _logger;

    public MoodService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IMapper mapper,IAlertNotifier? notifier=null,ILogger<MoodService>? logger=null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _notifier=notifier;_logger=logger;
    }

    public async Task<MoodEntryDto> RecordMoodAsync(
        int astronautId,
        CreateMoodEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Authorization: Monitors cannot write mood entries; Astronauts write own data only
        if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            throw new UnauthorizedAccessException("Flight monitors cannot create mood entries.");
        }

        if (!_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            throw new UnauthorizedAccessException("Only authenticated astronauts can create mood entries.");
        }

        var currentAstronautId = _currentUserService.AstronautId;
        if (currentAstronautId == null)
        {
            throw new UnauthorizedAccessException("Current astronaut identity could not be resolved from token claims.");
        }

        if (astronautId > 0 && astronautId != currentAstronautId.Value)
        {
            throw new UnauthorizedAccessException("Astronauts can only record mood entries for themselves.");
        }

        int effectiveAstronautId = currentAstronautId.Value;

        // 2. Idempotency: verify ClientGeneratedId
        var existingEntry = await _dbContext.MoodEntries
            .AsNoTracking()
            .Include(m => m.Astronaut)
            .FirstOrDefaultAsync(m => m.ClientGeneratedId == request.ClientGeneratedId, cancellationToken);

        if (existingEntry != null)
        {
            if(existingEntry.AstronautId!=effectiveAstronautId)
                throw new UnauthorizedAccessException("The submission ID belongs to a different astronaut.");
            return _mapper.Map<MoodEntryDto>(existingEntry);
        }

        var astronaut = await _dbContext.Astronauts.FindAsync(new object[] { effectiveAstronautId }, cancellationToken);
        if (astronaut == null)
        {
            throw new InvalidOperationException($"Astronaut with ID {effectiveAstronautId} does not exist.");
        }

        if (!astronaut.IsActive)
        {
            throw new InvalidOperationException($"Cannot record mood for deactivated astronaut {effectiveAstronautId}.");
        }

        // 3. Build entity
        var recordedAtUtc = DateTime.SpecifyKind(request.RecordedAt, DateTimeKind.Utc);

        var entry = new MoodEntry
        {
            ClientGeneratedId = request.ClientGeneratedId,
            AstronautId = effectiveAstronautId,
            RecordedAt = recordedAtUtc,
            MoodScore = request.MoodScore,
            MoodScaleMaximum = request.MoodScaleMaximum,
            StressLevel = request.StressLevel,
            DominantEmotion = request.DominantEmotion,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MoodEntries.Add(entry);
        entry.Astronaut=astronaut;
        var alert=await new WellnessRuleService(_dbContext).ApplyMoodAsync(entry,cancellationToken);
        try {await _dbContext.SaveChangesAsync(cancellationToken);}
        catch(DbUpdateException error) when(SubmissionConflict.IsDuplicate(error,"MoodEntries"))
        {
            _dbContext.ChangeTracker.Clear();
            var duplicate=await _dbContext.MoodEntries.AsNoTracking().Include(m=>m.Astronaut).SingleAsync(m=>m.ClientGeneratedId==request.ClientGeneratedId,cancellationToken);
            if(duplicate.AstronautId!=effectiveAstronautId)throw new UnauthorizedAccessException("The submission ID belongs to a different astronaut.");
            return _mapper.Map<MoodEntryDto>(duplicate);
        }
        if(alert is not null && _notifier is not null)
        {
            try {await _notifier.NotifyAlertCreatedAsync(_mapper.Map<AstronautHealthMonitoring.DTOs.Alerts.HealthAlertDto>(alert),cancellationToken);}
            catch(Exception e){_logger?.LogWarning(e,"Mood saved; live alert notification failed for astronaut {AstronautId}",effectiveAstronautId);}
        }

        entry.Astronaut = astronaut;

        return _mapper.Map<MoodEntryDto>(entry);
    }

    public async Task<MoodEntryDto?> GetMoodEntryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.MoodEntries
            .AsNoTracking()
            .Include(m => m.Astronaut)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (entry == null)
        {
            return null;
        }

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || entry.AstronautId != myAstronautId.Value)
            {
                return null;
            }
        }

        return _mapper.Map<MoodEntryDto>(entry);
    }

    public async Task<PagedResult<MoodEntryDto>> GetMoodHistoryPagedAsync(
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

        var query = _dbContext.MoodEntries
            .AsNoTracking()
            .Include(m => m.Astronaut)
            .Where(m => m.AstronautId == astronautId);

        if (fromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
            query = query.Where(m => m.RecordedAt >= from);
        }

        if (toUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc);
            query = query.Where(m => m.RecordedAt <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.RecordedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MoodEntryDto>
        {
            Items = _mapper.Map<IReadOnlyList<MoodEntryDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<MoodEntryDto>> GetMoodHistoryAsync(
        int astronautId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var paged = await GetMoodHistoryPagedAsync(astronautId, fromUtc, toUtc, 1, 100, cancellationToken);
        return paged.Items;
    }

    public async Task<MoodEntryDto?> GetLatestMoodEntryAsync(int astronautId, CancellationToken cancellationToken = default)
    {
        EnforceAstronautReadScope(astronautId);

        var entry = await _dbContext.MoodEntries
            .AsNoTracking()
            .Include(m => m.Astronaut)
            .Where(m => m.AstronautId == astronautId)
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entry != null ? _mapper.Map<MoodEntryDto>(entry) : null;
    }

    private void EnforceAstronautReadScope(int targetAstronautId)
    {
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || myAstronautId.Value != targetAstronautId)
            {
                throw new UnauthorizedAccessException("Astronauts can only view their own mood entries.");
            }
        }
    }
}

