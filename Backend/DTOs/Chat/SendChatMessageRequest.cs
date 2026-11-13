namespace AstronautHealthMonitoring.DTOs.Chat;

public class SendChatMessageRequest
{
    public Guid ClientGeneratedId { get; set; }
    public int? AstronautId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
