using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Predefined emergency procedures tailored for an astronaut without specialized clinical training.
/// Contains step-by-step instructions and immediate countermeasures.
/// </summary>
public class EmergencyProcedure : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g. "EP-HYPOXIA-01"
    public string Title { get; set; } = string.Empty;
    public string TargetCondition { get; set; } = string.Empty; // e.g. "Severe Hypoxia (SpO2 < 85%)"

    public EmergencySeverity Severity { get; set; } = EmergencySeverity.Critical;

    /// <summary>
    /// The single most urgent immediate action the astronaut must execute within seconds.
    /// </summary>
    public string ImmediateAction { get; set; } = string.Empty;

    /// <summary>
    /// Step-by-step numbered protocol formatted for quick cognitive execution under high stress.
    /// </summary>
    public string StepByStepInstructions { get; set; } = string.Empty;

    public string RequiredEquipment { get; set; } = string.Empty;

    /// <summary>
    /// Advisory notes from ground monitors or mission support.
    /// </summary>
    public string? GuidanceNotes { get; set; }

    // Navigation property
    public ICollection<HealthAlert> HealthAlerts { get; set; } = new List<HealthAlert>();
}
