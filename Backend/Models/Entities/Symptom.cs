using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Catalog of observable or subjective signs and symptoms.
/// Follows non-diagnostic phrasing standards.
/// </summary>
public class Symptom : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BodyArea { get; set; } = string.Empty;

    /// <summary>
    /// Critical red-flag indicator requiring immediate monitor notification and emergency triage.
    /// Eight red flags defined in the version 2 demonstration catalog.
    /// </summary>
    public bool IsRedFlag { get; set; }

    // Navigation property
    public ICollection<ConditionSymptom> ConditionSymptoms { get; set; } = new List<ConditionSymptom>();
}

