using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Astronaut sleep telemetry tracking circadian stability and sleep stages.
/// Supports offline-sync idempotency via ClientGeneratedId.
/// </summary>
public class SleepRecord : BaseEntity
{
    /// <summary>
    /// Client-generated GUID for offline-sync idempotency.
    /// </summary>
    public Guid ClientGeneratedId { get; set; } = Guid.NewGuid();

    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    /// <summary>
    /// Exact device timestamp when sleep record was logged by device/astronaut in orbit.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationHours { get; set; }

    public SleepQuality Quality { get; set; } = SleepQuality.Good;

    public int DeepSleepMinutes { get; set; }
    public int RemSleepMinutes { get; set; }
    public int LightSleepMinutes { get; set; }
    public int AwakeMinutes { get; set; }
    public double SleepEfficiencyPercentage { get; set; }

    public string? Notes { get; set; }
}
