using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Communication message exchanged between an astronaut and ground monitors.
/// Thread key is AstronautId (one shared thread per astronaut).
/// Supports offline synchronization via ClientGeneratedId and RecordedAt.
/// </summary>
public class ChatMessage : BaseEntity
{
    /// <summary>
    /// Client-generated GUID for offline-sync idempotency.
    /// </summary>
    public Guid ClientGeneratedId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Thread key: each astronaut has one unified support thread with ground monitors.
    /// </summary>
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public string SenderUserId { get; set; } = string.Empty;

    /// <summary>
    /// Role of the sender: "Astronaut" or "Monitor".
    /// </summary>
    public string SenderRole { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Device timestamp when the message was drafted/queued by the client.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Timestamp when message was received and persisted by the backend.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the recipient read the message. Nullable (unread until stamped).
    /// </summary>
    public DateTime? ReadAt { get; set; }
}

