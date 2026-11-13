using AstronautHealthMonitoring.DTOs.Alerts;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Abstraction for publishing real-time health alert lifecycle events across transport mechanisms (SignalR).
/// </summary>
public interface IAlertNotifier
{
    /// <summary>
    /// Broadcasts an alert creation event to the monitors group and the targeted astronaut's group.
    /// </summary>
    Task NotifyAlertCreatedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an alert acknowledgement event to the monitors group and the owning astronaut's group.
    /// </summary>
    Task NotifyAlertAcknowledgedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an alert resolution event to the monitors group and the owning astronaut's group.
    /// </summary>
    Task NotifyAlertResolvedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default);
}

