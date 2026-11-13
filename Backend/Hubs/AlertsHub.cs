using System.Security.Claims;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AstronautHealthMonitoring.Hubs;

/// <summary>
/// SignalR hub streaming real-time safety alerts to flight monitor consoles and astronaut dashboards.
/// Enforces JWT authentication and partitions users into targeted notification groups.
/// </summary>
[Authorize]
public class AlertsHub : Hub
{
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<AlertsHub> _logger;

    public const string MonitorsGroupName = "monitors";
    public static string GetAstronautGroupName(int astronautId) => $"astronaut-{astronautId}";

    public AlertsHub(ILogger<AlertsHub> logger, ICurrentUserService? currentUserService = null)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public override async Task OnConnectedAsync()
    {
        var role = _currentUserService?.Role ?? Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (string.Equals(role, UserRoles.Monitor, StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, MonitorsGroupName);
            _logger.LogInformation("Connection {ConnectionId} joined '{Group}' group.", Context.ConnectionId, MonitorsGroupName);
        }
        else if (string.Equals(role, UserRoles.Astronaut, StringComparison.OrdinalIgnoreCase))
        {
            var astronautId = _currentUserService?.AstronautId;
            if (!astronautId.HasValue)
            {
                var claimVal = Context.User?.FindFirst("astronaut_id")?.Value;
                if (int.TryParse(claimVal, out var parsed))
                {
                    astronautId = parsed;
                }
            }

            if (astronautId.HasValue)
            {
                var astronautGroup = GetAstronautGroupName(astronautId.Value);
                await Groups.AddToGroupAsync(Context.ConnectionId, astronautGroup);
                _logger.LogInformation("Connection {ConnectionId} joined '{Group}' group.", Context.ConnectionId, astronautGroup);
            }
            else
            {
                _logger.LogWarning("Astronaut connection {ConnectionId} lacked a valid astronaut_id claim.", Context.ConnectionId);
            }
        }

        await base.OnConnectedAsync();
    }
}

