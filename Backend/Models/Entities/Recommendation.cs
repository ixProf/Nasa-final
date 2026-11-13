using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Guidance generated automatically or by ground monitors for the astronaut.
/// Always explains the physiological 'Reason' behind each recommendation.
/// </summary>
public class Recommendation : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Physiological or operational reason explaining why this recommendation was given.
    /// Crucial for astronaut buy-in and clear understanding.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public RecommendationCategory Category { get; set; } = RecommendationCategory.Medical;
    public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;
    public RecommendationStatus Status { get; set; } = RecommendationStatus.Pending;

    public DateTime? ActionDueDate { get; set; }
    public bool IsDailyHabit { get; set; }
    public int HabitPoints { get; set; } = 10;
    public string? TitleAr { get; set; }
    public string? ContentAr { get; set; }
    public string? ReasonAr { get; set; }
    public string? ScheduledTime { get; set; }
    public string? PrescribedBy { get; set; }
}

