using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Verified medical background entry for an astronaut, reviewed by aerospace medicine specialists during flight certification.
/// Records pre-existing conditions, allergies, surgical history, and terrestrial baseline traits.
/// </summary>
public class MedicalHistoryEntry : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public string Category { get; set; } = string.Empty; // e.g. "Allergy", "SurgicalHistory", "PreExistingCondition", "FamilyHistory"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool IsPreExisting { get; set; } = true;
    public DateTime? DiagnosedDate { get; set; }

    /// <summary>
    /// Ground monitor operational recommendations or mission considerations for this condition.
    /// </summary>
    public string? MonitorNotes { get; set; }
}

