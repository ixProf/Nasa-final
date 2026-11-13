using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Operational medication regimen prescribed for the astronaut.
/// Directs drug administration schedules, dosage, route, and spaceflight pharmacology notes.
/// </summary>
public class Medication : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public string MedicationName { get; set; } = string.Empty; // e.g. "Scopolamine", "Zolpidem", "Acetaminophen", "Ciprofloxacin"
    public string Dosage { get; set; } = string.Empty; // e.g. "0.4 mg", "10 mg", "500 mg"
    public string Route { get; set; } = string.Empty; // e.g. "Oral", "Transdermal Patch", "Intramuscular", "Ophthalmic"
    public string Frequency { get; set; } = string.Empty; // e.g. "Every 8 hours as needed", "Once daily before sleep"

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Clinical indication or operational purpose (e.g. "Space Adaptation Sickness countermeasure", "Circadian adjustment").
    /// </summary>
    public string Indication { get; set; } = string.Empty;

    /// <summary>
    /// Explicit administration instructions for an astronaut (e.g. "Apply patch behind ear 2 hours prior to scheduled airlock prep").
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    public string PrescribedBy { get; set; } = string.Empty;
}

