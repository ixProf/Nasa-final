using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for querying, acknowledging, and managing health safety alerts.
/// </summary>
public interface IHealthAlertService
{
    /// <summary>
    /// Evaluates and generates a deterministic safety alert from a non-routine vital sign reading within the current transaction.
    /// </summary>
    Task<HealthAlertDto> CreateAlertFromReadingAsync(VitalSignReading reading, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually triggers a health safety alert from an astronaut terminal or flight monitor console.
    /// </summary>
    Task<HealthAlertDto> TriggerManualAlertAsync(TriggerManualAlertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated health alerts filtered optionally by status, severity, and astronaut.
    /// Enforces astronaut privacy scoping.
    /// </summary>
    Task<PagedResult<HealthAlertDto>> GetAlertsPagedAsync(AlertStatus? status, AlertSeverity? severity, int? astronautId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active safety alerts across all crew members for flight monitor consoles.
    /// </summary>
    Task<IReadOnlyList<HealthAlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves safety alerts for a specific astronaut, optionally filtered by active status.
    /// </summary>
    Task<IReadOnlyList<HealthAlertDto>> GetAlertsForAstronautAsync(int astronautId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single health alert by its unique database identifier.
    /// </summary>
    Task<HealthAlertDto?> GetAlertByIdAsync(int alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges an active health alert and logs the monitor action taken.
    /// </summary>
    Task<HealthAlertDto> AcknowledgeAlertAsync(int alertId, AcknowledgeAlertRequest request, string monitorUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an active or acknowledged health alert. Monitor role only.
    /// </summary>
    Task<HealthAlertDto> ResolveAlertAsync(int alertId, ResolveAlertRequest? request, string monitorUserId, CancellationToken cancellationToken = default);
}
