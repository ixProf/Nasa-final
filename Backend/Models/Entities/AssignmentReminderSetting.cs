namespace AstronautHealthMonitoring.Models.Entities;

public class AssignmentReminderSetting
{
    public int RecommendationId { get; set; }
    public Recommendation Recommendation { get; set; } = null!;
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 60;
    public bool SoundEnabled { get; set; } = true;
    public DateTime? NextDueAt { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}

// Retain only the latest delivery per assignment; offline users are not flooded with old reminders.
public class AssignmentReminderDelivery
{
    public int RecommendationId { get; set; }
    public Recommendation Recommendation { get; set; } = null!;
    public Guid Token { get; set; } = Guid.NewGuid();
    public DateTime SentAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool IsManual { get; set; }
    public bool SoundEnabled { get; set; }
    public string? SenderName { get; set; }
}
