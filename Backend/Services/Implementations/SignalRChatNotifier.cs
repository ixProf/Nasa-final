using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.Hubs;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class SignalRChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<SignalRChatNotifier> _logger;

    public const string ReceiveMessageMethod = "ReceiveMessage";
    public const string MessageReadMethod = "MessageRead";

    public SignalRChatNotifier(
        IHubContext<ChatHub> hubContext,
        ILogger<SignalRChatNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyMessageReceivedAsync(ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var astronautGroup = ChatHub.GetAstronautGroupName(message.AstronautId);
            await _hubContext.Clients.Groups(ChatHub.MonitorsGroupName, astronautGroup)
                .SendAsync(ReceiveMessageMethod, message, cancellationToken);

            _logger.LogInformation("SignalR notification sent for ReceiveMessage (MessageId: {MessageId}, AstronautId: {AstronautId}).",
                message.Id, message.AstronautId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notification for ReceiveMessage (MessageId: {MessageId})", message.Id);
        }
    }

    public async Task NotifyMessageReadAsync(ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var astronautGroup = ChatHub.GetAstronautGroupName(message.AstronautId);
            await _hubContext.Clients.Groups(ChatHub.MonitorsGroupName, astronautGroup)
                .SendAsync(MessageReadMethod, message, cancellationToken);

            _logger.LogInformation("SignalR notification sent for MessageRead (MessageId: {MessageId}, AstronautId: {AstronautId}).",
                message.Id, message.AstronautId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notification for MessageRead (MessageId: {MessageId})", message.Id);
        }
    }
}

