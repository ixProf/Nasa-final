using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.Hubs;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class SignalRAlertNotifier : IAlertNotifier
{
    private readonly IHubContext<AlertsHub> _hubContext;
    private readonly ILogger<SignalRAlertNotifier> _logger;

    public const string AlertCreatedMethod = "AlertCreated";
    public const string AlertAcknowledgedMethod = "AlertAcknowledged";
    public const string AlertResolvedMethod = "AlertResolved";

    public SignalRAlertNotifier(
        IHubContext<AlertsHub> hubContext,
        ILogger<SignalRAlertNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyAlertCreatedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert);

        try
        {
            var astronautGroup = AlertsHub.GetAstronautGroupName(alert.AstronautId);

            // Send to monitors and the owning astronaut
            await _hubContext.Clients.Groups(AlertsHub.MonitorsGroupName, astronautGroup)
                .SendAsync(AlertCreatedMethod, alert, cancellationToken);

            _logger.LogInformation("SignalR notification sent for AlertCreated (Alert ID: {AlertId}, Astronaut: {AstronautId}).",
                alert.Id, alert.AstronautId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR AlertCreated notification for Alert ID: {AlertId}. Execution will continue.",
                alert.Id);
        }
    }

    public async Task NotifyAlertAcknowledgedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert);

        try
        {
            var astronautGroup = AlertsHub.GetAstronautGroupName(alert.AstronautId);

            await _hubContext.Clients.Groups(AlertsHub.MonitorsGroupName, astronautGroup)
                .SendAsync(AlertAcknowledgedMethod, alert, cancellationToken);

            _logger.LogInformation("SignalR notification sent for AlertAcknowledged (Alert ID: {AlertId}).", alert.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR AlertAcknowledged notification for Alert ID: {AlertId}. Execution will continue.",
                alert.Id);
        }
    }

    public async Task NotifyAlertResolvedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert);

        try
        {
            var astronautGroup = AlertsHub.GetAstronautGroupName(alert.AstronautId);

            await _hubContext.Clients.Groups(AlertsHub.MonitorsGroupName, astronautGroup)
                .SendAsync(AlertResolvedMethod, alert, cancellationToken);

            _logger.LogInformation("SignalR notification sent for AlertResolved (Alert ID: {AlertId}).", alert.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR AlertResolved notification for Alert ID: {AlertId}. Execution will continue.",
                alert.Id);
        }
    }
}

