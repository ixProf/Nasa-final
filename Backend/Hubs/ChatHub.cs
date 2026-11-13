using System.Security.Claims;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Hubs;

/// <summary>
/// SignalR hub providing bi-directional real-time messaging between astronauts and flight monitors.
/// Enforces thread isolation: astronauts can only access their personal group, while monitors have mission oversight.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<ChatHub> _logger;

    public const string MonitorsGroupName = "monitors";
    public static string GetAstronautGroupName(int astronautId) => $"astronaut-{astronautId}";

    public ChatHub(
        IChatService chatService,
        ILogger<ChatHub> logger,
        ICurrentUserService? currentUserService = null)
    {
        _chatService = chatService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public override async Task OnConnectedAsync()
    {
        var role = _currentUserService?.Role ?? Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (string.Equals(role, UserRoles.Monitor, StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, MonitorsGroupName);
            _logger.LogInformation("ChatHub: Connection {ConnectionId} joined '{Group}' group.", Context.ConnectionId, MonitorsGroupName);
        }
        else if (string.Equals(role, UserRoles.Astronaut, StringComparison.OrdinalIgnoreCase))
        {
            var astronautId = GetCurrentAstronautId();
            if (astronautId.HasValue)
            {
                var astronautGroup = GetAstronautGroupName(astronautId.Value);
                await Groups.AddToGroupAsync(Context.ConnectionId, astronautGroup);
                _logger.LogInformation("ChatHub: Connection {ConnectionId} joined '{Group}' group.", Context.ConnectionId, astronautGroup);
            }
            else
            {
                _logger.LogWarning("ChatHub: Astronaut connection {ConnectionId} lacked a valid astronaut_id claim.", Context.ConnectionId);
            }
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Joins a specific astronaut conversation thread. Astronauts can only join their own thread.
    /// </summary>
    public async Task JoinThread(int astronautId)
    {
        var role = _currentUserService?.Role ?? Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (string.Equals(role, UserRoles.Astronaut, StringComparison.OrdinalIgnoreCase))
        {
            var myAstronautId = GetCurrentAstronautId();
            if (!myAstronautId.HasValue || myAstronautId.Value != astronautId)
            {
                throw new HubException("Astronauts cannot join threads of other astronauts.");
            }
        }

        var groupName = GetAstronautGroupName(astronautId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Sends a message to the astronaut conversation thread.
    /// </summary>
    public async Task<ChatMessageDto> SendMessage(SendChatMessageRequest request, int? targetAstronautId = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var role = _currentUserService?.Role ?? Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? UserRoles.Astronaut;
        var userId = _currentUserService?.UserId ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        int resolvedAstronautId;
        if (string.Equals(role, UserRoles.Astronaut, StringComparison.OrdinalIgnoreCase))
        {
            var myAstronautId = GetCurrentAstronautId();
            if (!myAstronautId.HasValue)
            {
                throw new HubException("Astronaut identity could not be resolved.");
            }
            resolvedAstronautId = myAstronautId.Value;
        }
        else
        {
            var target = targetAstronautId ?? request.AstronautId;
            if (!target.HasValue || target.Value <= 0)
            {
                throw new HubException("A valid target astronautId must be specified by monitors.");
            }
            resolvedAstronautId = target.Value;
        }

        return await _chatService.SendMessageAsync(resolvedAstronautId, request, userId, role);
    }

    /// <summary>
    /// Marks a message as read.
    /// </summary>
    public async Task<ChatMessageDto> MarkAsRead(int messageId)
    {
        var role = _currentUserService?.Role ?? Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? UserRoles.Astronaut;
        var userId = _currentUserService?.UserId ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        return await _chatService.MarkAsReadAsync(messageId, userId, role);
    }

    private int? GetCurrentAstronautId()
    {
        if (_currentUserService?.AstronautId.HasValue == true)
        {
            return _currentUserService.AstronautId.Value;
        }

        var claimVal = Context.User?.FindFirst("astronaut_id")?.Value;
        if (int.TryParse(claimVal, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}

