using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Sleep;

public class SleepRecordDto
{
    public int Id { get; set; }
    public Guid ClientGeneratedId { get; set; }
    public int AstronautId { get; set; }
    public string AstronautFullName { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationHours { get; set; }

    public SleepQuality Quality { get; set; }

    public int DeepSleepMinutes { get; set; }
    public int RemSleepMinutes { get; set; }
    public int LightSleepMinutes { get; set; }
    public int AwakeMinutes { get; set; }
    public double SleepEfficiencyPercentage { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
