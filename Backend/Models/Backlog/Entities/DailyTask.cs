using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Operational daily health and countermeasure routines assigned to the astronaut.
/// </summary>
public class DailyTask : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public DateOnly ScheduledDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public TaskType TaskType { get; set; } = TaskType.HealthCheck;
    public DailyTaskStatus Status { get; set; } = DailyTaskStatus.Pending;

    public DateTime? CompletedAt { get; set; }
}
