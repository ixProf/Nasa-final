using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Communication message exchanged within a MedicalSupportRequest thread.
/// Synchronized via SignalR ChatHub.
/// </summary>
public class MedicalMessage : BaseEntity
{
    public int MedicalSupportRequestId { get; set; }
    public MedicalSupportRequest MedicalSupportRequest { get; set; } = null!;

    public string SenderUserId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }

    public bool IsRead { get; set; } = false;
}

