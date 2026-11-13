using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Recommendations;

public class CreateRecommendationRequest
{
    public int AstronautId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public RecommendationCategory Category { get; set; } = RecommendationCategory.Medical;
    public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;
    public DateTime? ActionDueDate { get; set; }
    public bool IsDailyHabit { get; set; }
    public int HabitPoints { get; set; } = 10;
    public string? TitleAr { get; set; }
    public string? ContentAr { get; set; }
    public string? ReasonAr { get; set; }
    public string? ScheduledTime { get; set; }
    public string? PrescribedBy { get; set; }
    public bool ReminderEnabled { get; set; } = true;
    public int ReminderIntervalMinutes { get; set; } = 60;
    public bool ReminderSoundEnabled { get; set; } = true;
}

