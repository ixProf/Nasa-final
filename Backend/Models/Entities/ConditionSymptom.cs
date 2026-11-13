using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Associative mapping between a SpaceCondition and a Symptom.
/// </summary>
public class ConditionSymptom : BaseEntity
{
    public int ConditionId { get; set; }
    public SpaceCondition Condition { get; set; } = null!;

    public int SymptomId { get; set; }
    public Symptom Symptom { get; set; } = null!;

    public int Weight { get; set; } = 1;
}

