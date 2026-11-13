using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Dashboard;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        AppDbContext dbContext,
        IMapper mapper,
        ILogger<DashboardService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<MonitorDashboardSummaryDto> GetMonitorDashboardAsync(CancellationToken cancellationToken = default)
    {
        // 1. Fetch all astronauts and their missions (Single query, AsNoTracking)
        var astronauts = await _dbContext.Astronauts
            .Include(a => a.Mission)
            .AsNoTracking()
            .OrderBy(a => a.AstronautCode)
            .ToListAsync(cancellationToken);

        // 2. Fetch all missions (Single query, AsNoTracking)
        var missions = await _dbContext.Missions
            .Include(m => m.Astronauts)
            .AsNoTracking()
            .OrderBy(m => m.StartDate)
            .ToListAsync(cancellationToken);

        // 3. Fetch all active alerts (Single query, AsNoTracking)
        var activeAlerts = await _dbContext.HealthAlerts
            .Include(a => a.Astronaut)
            .Include(a => a.VitalSignReading!)
                .ThenInclude(r => r.VitalSignType)
            .Include(a => a.EmergencyProcedure)
            .AsNoTracking()
            .Where(a => a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(cancellationToken);

        // 4. Fetch vital sign readings (Single query, AsNoTracking)
        var allReadings = await _dbContext.VitalSignReadings
            .LatestPerMetric()
            .Include(r => r.VitalSignType)
            .AsNoTracking()
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(cancellationToken);

        // In-memory grouping: latest reading per (AstronautId, VitalSignTypeId)
        var latestReadingsByAstronaut = allReadings
            .GroupBy(r => (r.AstronautId, r.VitalSignTypeId))
            .Select(g => g.First())
            .GroupBy(r => r.AstronautId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.VitalSignTypeId).ToList());

        // In-memory grouping: active alerts per AstronautId
        var alertsByAstronaut = activeAlerts
            .GroupBy(a => a.AstronautId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Deactivated astronauts: flagged with IsActive=false and excluded from active alert counts
        var activeCrewSet = astronauts.Where(a => a.IsActive).Select(a => a.Id).ToHashSet();
        var activeCrewAlerts = activeAlerts.Where(a => activeCrewSet.Contains(a.AstronautId)).ToList();

        var crewSummaries = new List<AstronautStatusSummaryDto>(astronauts.Count);

        foreach (var astro in astronauts)
        {
            var readings = latestReadingsByAstronaut.GetValueOrDefault(astro.Id) ?? new();
            var alerts = alertsByAstronaut.GetValueOrDefault(astro.Id) ?? new();

            // Evaluate overall status across latest vitals and active alerts
            var overallStatus = VitalSignStatus.Routine;
            if (alerts.Any(a => a.Severity == AlertSeverity.Emergency) || readings.Any(r => r.Status == VitalSignStatus.Emergency))
            {
                overallStatus = VitalSignStatus.Emergency;
            }
            else if (alerts.Any(a => a.Severity == AlertSeverity.MedicalAttention) || readings.Any(r => r.Status == VitalSignStatus.MedicalAttention))
            {
                overallStatus = VitalSignStatus.MedicalAttention;
            }

            var readingDtos = _mapper.Map<List<VitalSignReadingDto>>(readings);

            crewSummaries.Add(new AstronautStatusSummaryDto
            {
                AstronautId = astro.Id,
                FullName = astro.FullName,
                AstronautCode = astro.AstronautCode,
                MissionName = astro.Mission?.Name,
                IsActive = astro.IsActive,
                OverallStatus = overallStatus,
                // If deactivated, exclude from alert counts per policy
                ActiveAlertsCount = astro.IsActive ? alerts.Count : 0,
                EmergencyAlertsCount = astro.IsActive ? alerts.Count(a => a.Severity == AlertSeverity.Emergency) : 0,
                MedicalAttentionAlertsCount = astro.IsActive ? alerts.Count(a => a.Severity == AlertSeverity.MedicalAttention) : 0,
                LastReadingRecordedAt = readings.FirstOrDefault()?.RecordedAt,
                LatestVitals = readingDtos
            });
        }

        var missionSummaries = missions.Select(m => new MissionSummaryDto
        {
            Id = m.Id,
            Code = m.Code,
            Name = m.Name,
            Destination = m.Destination,
            Status = m.Status,
            CrewCount = m.Astronauts.Count(a => a.IsActive)
        }).ToList();

        var criticalAlertDtos = _mapper.Map<List<HealthAlertDto>>(
            activeCrewAlerts.Where(a => a.Severity == AlertSeverity.Emergency).Take(10).ToList());

        return new MonitorDashboardSummaryDto
        {
            TotalMonitoredAstronauts = astronauts.Count(a => a.IsActive),
            ActiveAstronautsCount = astronauts.Count(a => a.IsActive),
            DeactivatedAstronautsCount = astronauts.Count(a => !a.IsActive),
            ActiveMissionsCount = missions.Count(m => m.Status == MissionStatus.Active),
            ActiveAlertsCount = activeCrewAlerts.Count,
            EmergencyAlertsCount = activeCrewAlerts.Count(a => a.Severity == AlertSeverity.Emergency),
            MedicalAttentionAlertsCount = activeCrewAlerts.Count(a => a.Severity == AlertSeverity.MedicalAttention),
            MissionSummaries = missionSummaries,
            CriticalAlerts = criticalAlertDtos,
            CrewSummaries = crewSummaries
        };
    }

    public async Task<AstronautDashboardSummaryDto?> GetAstronautSnapshotAsync(int astronautId, CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts
            .Include(a => a.Mission)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);

        if (astronaut == null)
        {
            return null;
        }

        // 1. Latest vitals per metric
        var readings = await _dbContext.VitalSignReadings
            .LatestPerMetric()
            .Include(r => r.VitalSignType)
            .AsNoTracking()
            .Where(r => r.AstronautId == astronautId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(cancellationToken);

        var latestReadings = readings
            .GroupBy(r => r.VitalSignTypeId)
            .Select(g => g.First())
            .OrderBy(r => r.VitalSignTypeId)
            .ToList();

        // 2. Active alerts
        var activeAlerts = await _dbContext.HealthAlerts
            .Include(a => a.EmergencyProcedure)
            .Include(a => a.VitalSignReading!)
                .ThenInclude(r => r.VitalSignType)
            .AsNoTracking()
            .Where(a => a.AstronautId == astronautId && a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.TriggeredAt)
            .ToListAsync(cancellationToken);

        // Overall status
        var overallStatus = VitalSignStatus.Routine;
        if (activeAlerts.Any(a => a.Severity == AlertSeverity.Emergency) || latestReadings.Any(r => r.Status == VitalSignStatus.Emergency))
        {
            overallStatus = VitalSignStatus.Emergency;
        }
        else if (activeAlerts.Any(a => a.Severity == AlertSeverity.MedicalAttention) || latestReadings.Any(r => r.Status == VitalSignStatus.MedicalAttention))
        {
            overallStatus = VitalSignStatus.MedicalAttention;
        }

        // 3. Last sleep record
        var latestSleep = await _dbContext.SleepRecords
            .AsNoTracking()
            .Where(s => s.AstronautId == astronautId)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        // 4. Recent mood entry
        var latestMood = await _dbContext.MoodEntries
            .AsNoTracking()
            .Where(m => m.AstronautId == astronautId)
            .OrderByDescending(m => m.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // 5. Pending recommendations
        var pendingRecs = await _dbContext.Recommendations
            .Include(r => r.Astronaut)
            .AsNoTracking()
            .Where(r => r.AstronautId == astronautId && r.Status == RecommendationStatus.Pending)
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        // 6. Unread chat count from Ground Support
        var unreadChatCount = await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(c => c.AstronautId == astronautId && c.ReadAt == null && c.SenderRole == UserRoles.Monitor)
            .CountAsync(cancellationToken);

        return new AstronautDashboardSummaryDto
        {
            AstronautId = astronaut.Id,
            FullName = astronaut.FullName,
            AstronautCode = astronaut.AstronautCode,
            MissionName = astronaut.Mission?.Name,
            IsActive = astronaut.IsActive,
            OverallStatus = overallStatus,
            LastSyncAt = latestReadings.FirstOrDefault()?.RecordedAt ?? astronaut.UpdatedAt ?? astronaut.CreatedAt,
            RecentVitals = _mapper.Map<List<VitalSignReadingDto>>(latestReadings),
            ActiveAlerts = _mapper.Map<List<HealthAlertDto>>(activeAlerts),
            LatestSleep = latestSleep != null ? _mapper.Map<SleepRecordDto>(latestSleep) : null,
            LatestMood = latestMood != null ? _mapper.Map<MoodEntryDto>(latestMood) : null,
            PendingRecommendations = _mapper.Map<List<RecommendationDto>>(pendingRecs),
            UnreadChatMessagesCount = unreadChatCount
        };
    }

    public async Task<AstronautDashboardSummaryDto> GetMyDashboardAsync(int astronautId, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetAstronautSnapshotAsync(astronautId, cancellationToken);
        if (snapshot == null)
        {
            throw new KeyNotFoundException($"Astronaut profile with ID {astronautId} not found.");
        }

        return snapshot;
    }
}
