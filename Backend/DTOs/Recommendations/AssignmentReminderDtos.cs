using System.ComponentModel.DataAnnotations;

namespace AstronautHealthMonitoring.DTOs.Recommendations;

public class UpdateAssignmentReminderRequest
{
    public bool Enabled { get; set; }
    [Range(1, 10080)]
    public int IntervalMinutes { get; set; } = 60;
    public bool SoundEnabled { get; set; } = true;
}

public record AssignmentReminderDto(int RecommendationId, string Title, string? TitleAr,
    string Content, string? ContentAr, bool Enabled, int IntervalMinutes, bool SoundEnabled,
    DateTime? NextDueAt, bool CompletedToday);

public record AssignmentReminderDeliveryDto(int RecommendationId, int AstronautId, Guid Token,
    string Title, string? TitleAr, string Content, string? ContentAr, DateTime SentAt,
    bool IsManual, bool SoundEnabled, string? SenderName);

public record AssignmentRemindersDto(List<AssignmentReminderDto> Assignments, List<AssignmentReminderDeliveryDto> Pending);
