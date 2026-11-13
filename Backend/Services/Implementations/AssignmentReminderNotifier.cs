using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AstronautHealthMonitoring.Services.Implementations;

public interface IAssignmentReminderNotifier
{
    Task NotifyAsync(AssignmentReminderDeliveryDto delivery, CancellationToken ct = default);
}

public class AssignmentReminderNotifier(IHubContext<AlertsHub> hubs, ILogger<AssignmentReminderNotifier> logger) : IAssignmentReminderNotifier
{
    public async Task NotifyAsync(AssignmentReminderDeliveryDto delivery, CancellationToken ct = default)
    {
        try { await hubs.Clients.Group(AlertsHub.GetAstronautGroupName(delivery.AstronautId)).SendAsync("AssignmentReminder", delivery, ct); }
        catch (Exception e)
        {
            // The durable inbox remains available when live transport fails.
            logger.LogWarning(e, "Live reminder delivery unavailable for assignment {Id}; retained in inbox", delivery.RecommendationId);
        }
    }
}
