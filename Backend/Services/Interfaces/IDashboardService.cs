using AstronautHealthMonitoring.DTOs.Dashboard;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for aggregating crew telemetry, clinical safety alerts, and operational mission overviews.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Generates a comprehensive ground-monitor overview of crew vitals, active alerts, and missions.
    /// </summary>
    Task<MonitorDashboardSummaryDto> GetMonitorDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a full health and operational telemetry snapshot for a specific astronaut (Monitor oversight).
    /// </summary>
    Task<AstronautDashboardSummaryDto?> GetAstronautSnapshotAsync(int astronautId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the personal health and operational dashboard for the authenticated astronaut.
    /// </summary>
    Task<AstronautDashboardSummaryDto> GetMyDashboardAsync(int astronautId, CancellationToken cancellationToken = default);
}

