using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Recommendations;

public class RecommendationDto
{
    public int Id { get; set; }
    public int AstronautId { get; set; }
    public string AstronautFullName { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public RecommendationCategory Category { get; set; }
    public RecommendationPriority Priority { get; set; }
    public RecommendationStatus Status { get; set; }

    public DateTime? ActionDueDate { get; set; }
    public bool IsDailyHabit { get; set; }
    public int HabitPoints { get; set; }
    public string? TitleAr { get; set; }
    public string? ContentAr { get; set; }
    public string? ReasonAr { get; set; }
    public string ScheduleType => IsDailyHabit ? "DailyRecurring" : "Once";
    public string? ScheduledTime { get; set; }
    public string? PrescribedBy { get; set; }
    public string Source => PrescribedBy?.StartsWith("Automatic rule: ",StringComparison.Ordinal)==true ? "Automatic" : "Monitor";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
