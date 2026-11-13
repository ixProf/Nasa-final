using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Services.Implementations;

public class HealthAlertService : IHealthAlertService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IAlertNotifier? _alertNotifier;
    private readonly ILogger<HealthAlertService>? _logger;

    public HealthAlertService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IAlertNotifier? alertNotifier = null,
        ILogger<HealthAlertService>? logger = null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _alertNotifier = alertNotifier;
        _logger = logger;
    }

    public async Task<HealthAlertDto> CreateAlertFromReadingAsync(VitalSignReading reading, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reading);

        // Idempotency: verify no duplicate alert exists for this reading
        if (reading.Id != 0)
        {
            var existingAlert = await _dbContext.HealthAlerts
                .Include(a => a.Astronaut)
                .Include(a => a.VitalSignReading)
                    .ThenInclude(r => r!.VitalSignType)
                .Include(a => a.EmergencyProcedure)
                .FirstOrDefaultAsync(a => a.VitalSignReadingId == reading.Id, cancellationToken);

            if (existingAlert != null)
            {
                return _mapper.Map<HealthAlertDto>(existingAlert);
            }
        }

        var severity = reading.Status == VitalSignStatus.Emergency
            ? AlertSeverity.Emergency
            : AlertSeverity.MedicalAttention;

        // Ensure VitalSignType is loaded for metric details
        var vitalSignType = reading.VitalSignType;
        if (vitalSignType == null && reading.VitalSignTypeId != 0)
        {
            vitalSignType = await _dbContext.VitalSignTypes.FindAsync(new object[] { reading.VitalSignTypeId }, cancellationToken);
        }

        var metricName = vitalSignType?.Name ?? "Vital Sign";
        var metricCode = vitalSignType?.Code ?? string.Empty;
        var unit = vitalSignType?.Unit ?? string.Empty;

        // Emergency Procedure matching for Emergency severity
        int? emergencyProcedureId = null;
        EmergencyProcedure? matchedProcedure = null;

        if (severity == AlertSeverity.Emergency)
        {
            if (metricCode.Equals("SPO2", StringComparison.OrdinalIgnoreCase))
            {
                matchedProcedure = await _dbContext.EmergencyProcedures
                    .FirstOrDefaultAsync(p => p.Code == "EP-HYPOXIA-01", cancellationToken);
            }
            else if (metricCode.Equals("RESP", StringComparison.OrdinalIgnoreCase))
            {
                matchedProcedure = await _dbContext.EmergencyProcedures
                    .FirstOrDefaultAsync(p => p.Code == "EP-CO2-02", cancellationToken);
            }
            else
            {
                matchedProcedure = await _dbContext.EmergencyProcedures
                    .FirstOrDefaultAsync(p => p.TargetCondition.ToLower().Contains(metricName.ToLower()), cancellationToken);
            }

            emergencyProcedureId = matchedProcedure?.Id;
        }

        // Determine title, message, reason, and action required
        var title = $"{metricName} {severity} Alert";
        var readingValueStr = reading.SecondaryValue.HasValue
            ? $"{reading.Value:F1}/{reading.SecondaryValue.Value:F1} {unit}".Trim()
            : $"{reading.Value:F1} {unit}".Trim();

        var message = $"{metricName} reading of {readingValueStr} recorded with status {reading.Status}.";
        var reason = $"Measurement breached clinical thresholds ({reading.Status}) during {reading.ActivityContext} context.";

        string actionRequired;
        if (severity == AlertSeverity.Emergency && matchedProcedure != null)
        {
            actionRequired = $"Immediate action: {matchedProcedure.ImmediateAction} Follow procedure {matchedProcedure.Code}: {matchedProcedure.Title}.";
        }
        else if (severity == AlertSeverity.Emergency)
        {
            actionRequired = "Immediate emergency response required. Don emergency breathing gear if indicated and alert ground monitor.";
        }
        else
        {
            actionRequired = "Log context, rest, and repeat measurement in 15 minutes. Notify flight surgeon if abnormal state persists.";
        }

        var alert = new HealthAlert
        {
            AstronautId = reading.AstronautId,
            VitalSignReadingId = reading.Id != 0 ? reading.Id : null,
            VitalSignReading = reading.Id == 0 ? reading : null,
            EmergencyProcedureId = emergencyProcedureId,
            Severity = severity,
            Title = title,
            Message = message,
            Reason = reason,
            ActionRequired = actionRequired,
            TriggeredAt = reading.RecordedAt != default ? reading.RecordedAt : DateTime.UtcNow,
            Status = AlertStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.HealthAlerts.Add(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Reload navigational links if needed for DTO mapping
        if (alert.Astronaut == null)
        {
            var loadedAstronaut = await _dbContext.Astronauts.FindAsync(new object[] { alert.AstronautId }, cancellationToken);
            if (loadedAstronaut != null)
            {
                alert.Astronaut = loadedAstronaut;
            }
        }
        if (alert.EmergencyProcedure == null && emergencyProcedureId.HasValue)
        {
            alert.EmergencyProcedure = matchedProcedure;
        }
        if (alert.VitalSignReading == null && alert.VitalSignReadingId.HasValue)
        {
            alert.VitalSignReading = reading;
        }

        return _mapper.Map<HealthAlertDto>(alert);
    }

    public async Task<HealthAlertDto> TriggerManualAlertAsync(TriggerManualAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        int targetAstronautId;

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                throw new UnauthorizedAccessException("Current astronaut identity could not be resolved.");
            }

            if (request.AstronautId != 0 && request.AstronautId != myAstronautId.Value)
            {
                throw new UnauthorizedAccessException("Astronauts can only trigger manual alerts for themselves.");
            }

            targetAstronautId = myAstronautId.Value;
        }
        else if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            if (request.AstronautId <= 0)
            {
                throw new InvalidOperationException("A valid target AstronautId must be specified by monitors.");
            }

            targetAstronautId = request.AstronautId;
        }
        else
        {
            throw new UnauthorizedAccessException("User is not authorized to trigger safety alerts.");
        }

        var astronaut = await _dbContext.Astronauts.FirstOrDefaultAsync(a => a.Id == targetAstronautId, cancellationToken);
        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {targetAstronautId} does not exist.");
        }

        if (!astronaut.IsActive)
        {
            throw new InvalidOperationException($"Astronaut '{astronaut.AstronautCode}' is deactivated and cannot trigger or receive alerts.");
        }

        // Serialize assessment creation for this astronaut, including across server instances.
        // No schema change is needed: the existing source, message and severity identify the issue.
        await using var transaction = request.ReuseOpenAssessmentAlert && _dbContext.Database.IsNpgsql()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        if (transaction != null)
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT \"Id\" FROM \"Astronauts\" WHERE \"Id\" = {targetAstronautId} FOR UPDATE", cancellationToken);

        if (request.ReuseOpenAssessmentAlert)
        {
            var existing = await _dbContext.HealthAlerts
                .Include(a => a.Astronaut).Include(a => a.EmergencyProcedure)
                .Where(a => a.AstronautId == targetAstronautId && a.Status != AlertStatus.Resolved
                    && a.Title == request.Title && a.Message == request.Message
                    && a.Reason == request.Reason && a.Severity == request.Severity)
                .OrderBy(a => a.TriggeredAt).ThenBy(a => a.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing != null)
            {
                if (transaction != null) await transaction.CommitAsync(cancellationToken);
                return _mapper.Map<HealthAlertDto>(existing);
            }
        }

        var alert = new HealthAlert
        {
            AstronautId = targetAstronautId,
            Severity = request.Severity,
            Title = request.Title,
            Message = request.Message,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Manually initiated safety alert." : request.Reason,
            ActionRequired = string.IsNullOrWhiteSpace(request.ActionRequired) ? "Acknowledge alert and conduct immediate crew assessment." : request.ActionRequired,
            EmergencyProcedureId = request.EmergencyProcedureId,
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.HealthAlerts.Add(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction != null) await transaction.CommitAsync(cancellationToken);

        // Load navigations for complete DTO
        await _dbContext.Entry(alert).Reference(a => a.Astronaut).LoadAsync(cancellationToken);
        if (alert.EmergencyProcedureId.HasValue)
        {
            await _dbContext.Entry(alert).Reference(a => a.EmergencyProcedure).LoadAsync(cancellationToken);
        }

        var dto = _mapper.Map<HealthAlertDto>(alert);

        if (_alertNotifier != null)
        {
            try
            {
                await _alertNotifier.NotifyAlertCreatedAsync(dto, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Alert notification failed for manual alert ID: {AlertId}.", alert.Id);
            }
        }

        return dto;
    }

    public async Task<PagedResult<HealthAlertDto>> GetAlertsPagedAsync(
        AlertStatus? status,
        AlertSeverity? severity,
        int? astronautId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        int? targetAstronautId;

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                throw new UnauthorizedAccessException("Current astronaut identity could not be resolved.");
            }

            if (astronautId.HasValue && astronautId.Value != myAstronautId.Value)
            {
                throw new UnauthorizedAccessException("Astronauts can only view their own alerts.");
            }

            targetAstronautId = myAstronautId.Value;
        }
        else
        {
            // Monitor can filter by astronautId or view all
            targetAstronautId = astronautId;
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.HealthAlerts
            .AsNoTracking()
            .Include(a => a.Astronaut)
            .Include(a => a.VitalSignReading)
                .ThenInclude(r => r!.VitalSignType)
            .Include(a => a.EmergencyProcedure)
            .AsQueryable();

        if (targetAstronautId.HasValue)
        {
            query = query.Where(a => a.AstronautId == targetAstronautId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(a => a.Severity == severity.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.TriggeredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<HealthAlertDto>
        {
            Items = _mapper.Map<IReadOnlyList<HealthAlertDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<HealthAlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAlertsPagedAsync(AlertStatus.Active, null, null, 1, 100, cancellationToken);
        return result.Items;
    }

    public async Task<IReadOnlyList<HealthAlertDto>> GetAlertsForAstronautAsync(int astronautId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        AlertStatus? statusFilter = activeOnly ? AlertStatus.Active : null;
        var result = await GetAlertsPagedAsync(statusFilter, null, astronautId, 1, 100, cancellationToken);
        return result.Items;
    }

    public async Task<HealthAlertDto?> GetAlertByIdAsync(int alertId, CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.HealthAlerts
            .AsNoTracking()
            .Include(a => a.Astronaut)
            .Include(a => a.VitalSignReading)
                .ThenInclude(r => r!.VitalSignType)
            .Include(a => a.EmergencyProcedure)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert == null)
        {
            return null;
        }

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || alert.AstronautId != myAstronautId.Value)
            {
                return null;
            }
        }

        return _mapper.Map<HealthAlertDto>(alert);
    }

    public async Task<HealthAlertDto> AcknowledgeAlertAsync(int alertId, AcknowledgeAlertRequest request, string monitorUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserService.IsInRole(UserRoles.Monitor))
        {
            throw new UnauthorizedAccessException("Only flight monitors can acknowledge health alerts.");
        }

        var alert = await _dbContext.HealthAlerts
            .Include(a => a.Astronaut)
            .Include(a => a.VitalSignReading)
                .ThenInclude(r => r!.VitalSignType)
            .Include(a => a.EmergencyProcedure)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert == null)
        {
            throw new KeyNotFoundException($"HealthAlert with ID {alertId} not found.");
        }

        // A delayed acknowledge request must never reopen a resolved alert.
        if (alert.Status != AlertStatus.Active) return _mapper.Map<HealthAlertDto>(alert);
        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AcknowledgedBy = !string.IsNullOrWhiteSpace(monitorUserId) ? monitorUserId : _currentUserService.UserId;
        alert.ActionTaken = request.ActionTaken;
        alert.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<HealthAlertDto>(alert);

        if (_alertNotifier != null)
        {
            try
            {
                await _alertNotifier.NotifyAlertAcknowledgedAsync(dto, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Alert notification failed for acknowledged alert ID: {AlertId}.", alert.Id);
            }
        }

        return dto;
    }

    public async Task<HealthAlertDto> ResolveAlertAsync(int alertId, ResolveAlertRequest? request, string monitorUserId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsInRole(UserRoles.Monitor))
        {
            throw new UnauthorizedAccessException("Only flight monitors can resolve health alerts.");
        }

        var alert = await _dbContext.HealthAlerts
            .Include(a => a.Astronaut)
            .Include(a => a.VitalSignReading)
                .ThenInclude(r => r!.VitalSignType)
            .Include(a => a.EmergencyProcedure)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert == null)
        {
            throw new KeyNotFoundException($"HealthAlert with ID {alertId} not found.");
        }

        if (alert.Status == AlertStatus.Resolved) return _mapper.Map<HealthAlertDto>(alert);

        var alertsToResolve = new List<HealthAlert> { alert };
        var isAssessment =
            (alert.Title == "Red-Flag Symptom Escalation" && alert.Reason == "Automated escalation triggered by symptom guidance assessment detecting red-flag clinical signs.") ||
            (alert.Title == "Critical Assessment Finding" && alert.Reason == "A matching symptom pattern has a related recent emergency vital reading.");
        if (isAssessment)
        {
            // Preserve legacy duplicates in history, closing only this exact issue for this astronaut.
            alertsToResolve.AddRange(await _dbContext.HealthAlerts
                .Include(a => a.Astronaut).Include(a => a.EmergencyProcedure)
                .Where(a => a.Id != alert.Id && a.AstronautId == alert.AstronautId
                    && a.Status != AlertStatus.Resolved && a.Title == alert.Title
                    && a.Message == alert.Message && a.Reason == alert.Reason && a.Severity == alert.Severity)
                .ToListAsync(cancellationToken));
        }

        foreach (var item in alertsToResolve)
        {
            item.Status = AlertStatus.Resolved;
            if (!item.AcknowledgedAt.HasValue)
            {
                item.AcknowledgedAt = DateTime.UtcNow;
                item.AcknowledgedBy = !string.IsNullOrWhiteSpace(monitorUserId) ? monitorUserId : _currentUserService.UserId;
            }
            if (!string.IsNullOrWhiteSpace(request?.ResolutionNotes))
                item.ActionTaken = string.IsNullOrWhiteSpace(item.ActionTaken)
                    ? request.ResolutionNotes : $"{item.ActionTaken} | Resolved: {request.ResolutionNotes}";
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var resolvedDto = _mapper.Map<HealthAlertDto>(alert);

        if (_alertNotifier != null)
        {
            try
            {
                foreach (var item in alertsToResolve)
                    await _alertNotifier.NotifyAlertResolvedAsync(_mapper.Map<HealthAlertDto>(item), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Alert notification failed for resolved alert ID: {AlertId}.", alert.Id);
            }
        }

        return resolvedDto;
    }
}
