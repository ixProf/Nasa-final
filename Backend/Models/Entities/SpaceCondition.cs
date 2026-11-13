using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Operational spaceflight physiological pattern or environment adaptation condition.
/// Follows strict non-diagnostic language: describes associations and supportive actions, never diagnoses diseases.
/// </summary>
public class SpaceCondition : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Narrative description of the physiological association (uses 'may be associated with').
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Supportive crew actions (no medication names, no doses, no invented numbers).
    /// </summary>
    public string SelfCareGuidance { get; set; } = string.Empty;

    /// <summary>
    /// Standard monitor escalation trigger guidance.
    /// </summary>
    public string WhenToContactMonitor { get; set; } = string.Empty;

    public string SourceReference { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this condition can be matched from crew-reported symptoms and wearable vitals.
    /// Set to false for knowledge-only conditions that require specialized medical equipment (e.g. Bone Loss, VTE).
    /// </summary>
    public bool IsMatchable { get; set; } = true;

    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ConditionSymptom> ConditionSymptoms { get; set; } = new List<ConditionSymptom>();
    public ICollection<ConditionVitalSign> ConditionVitalSigns { get; set; } = new List<ConditionVitalSign>();
}

/*

one to many

many to many

junction - bridge 

Astrunt one to many ->junction table <- one to many missions 



*/