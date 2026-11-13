using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Associative mapping between a SpaceCondition and an expected vital sign deviation direction.
/// </summary>
public class ConditionVitalSign : BaseEntity
{
    public int ConditionId { get; set; }
    public SpaceCondition Condition { get; set; } = null!;

    public int VitalSignTypeId { get; set; }
    public VitalSignType VitalSignType { get; set; } = null!;

    public VitalSignDirection Direction { get; set; } = VitalSignDirection.Either;

    /// <summary>
    /// Operational context for interpreting this vital sign deviation (e.g. 'mainly when standing', 'interpret with meal context').
    /// </summary>
    public string? ContextNote { get; set; }
}

