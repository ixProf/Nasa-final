using AstronautHealthMonitoring.DTOs.Chat;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Abstraction for publishing real-time chat messages and read receipts across transport mechanisms (SignalR).
/// </summary>
public interface IChatNotifier
{
    /// <summary>
    /// Broadcasts a received chat message to the monitors group and the astronaut's private thread group.
    /// </summary>
    Task NotifyMessageReceivedAsync(ChatMessageDto message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a message read receipt to the monitors group and the astronaut's private thread group.
    /// </summary>
    Task NotifyMessageReadAsync(ChatMessageDto message, CancellationToken cancellationToken = default);
}

