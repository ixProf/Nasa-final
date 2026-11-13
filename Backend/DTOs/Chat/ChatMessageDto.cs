namespace AstronautHealthMonitoring.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }
    public Guid ClientGeneratedId { get; set; }
    public int AstronautId { get; set; }
    public string AstronautFullName { get; set; } = string.Empty;

    public string SenderUserId { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;

    public DateTime CreatedAt { get; set; }
}
