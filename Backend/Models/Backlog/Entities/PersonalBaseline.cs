using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Individual astronaut baseline values computed from historical mission/pre-flight data.
/// Accounts for individual cardiovascular and metabolic adaptation in microgravity.
/// </summary>
public class PersonalBaseline : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public int VitalSignTypeId { get; set; }
    public VitalSignType VitalSignType { get; set; } = null!;

    /// <summary>
    /// Physiological epoch for baseline:
    /// PreFlight (terrestrial 1g baseline), InFlight (microgravity-adapted baseline), Current (rolling moving window).
    /// </summary>
    public BaselineType BaselineType { get; set; } = BaselineType.Current;

    public double BaselineValue { get; set; }
    public double? BaselineSecondaryValue { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public int SampleCount { get; set; }
    public double? StandardDeviation { get; set; }
}
