using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Catalog of microgravity physical exercise types (e.g. T2 Treadmill, CEVIS cycle, ARED resistance, EVA).
/// Essential for mitigating muscle atrophy and bone mineral density loss.
/// </summary>
public class PhysicalActivity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ActivityCategory Category { get; set; } = ActivityCategory.Cardio;
    public string Description { get; set; } = string.Empty;
    public int RecommendedDurationMinutes { get; set; }

    // Navigation properties
    public ICollection<ExerciseLog> ExerciseLogs { get; set; } = new List<ExerciseLog>();
}

