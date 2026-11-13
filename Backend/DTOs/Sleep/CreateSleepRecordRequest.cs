using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Sleep;

public class CreateSleepRecordRequest
{
    public Guid ClientGeneratedId { get; set; }

    public DateTime RecordedAt { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public SleepQuality Quality { get; set; } = SleepQuality.Good;

    public int DeepSleepMinutes { get; set; }
    public int RemSleepMinutes { get; set; }
    public int LightSleepMinutes { get; set; }
    public int AwakeMinutes { get; set; }
    public double SleepEfficiencyPercentage { get; set; }

    public string? Notes { get; set; }
}
