using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Astronaut self-reported symptom entry (e.g. Space Motion Sickness / SAS, cephalad fluid shift headache, back pain, visual changes).
/// Links optionally to concurrent vital sign readings to aid ground monitors in correlating symptoms with telemetry.
/// Principle: The system logs subjective symptoms and observable signs; it does not assign disease diagnoses.
/// </summary>
public class SymptomLog : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public string SymptomType { get; set; } = string.Empty; // e.g. "Space Adaptation Sickness", "Headache", "Visual Blurring", "Lumbar Discomfort"
    public SymptomSeverity Severity { get; set; } = SymptomSeverity.Mild;

    public DateTime OnsetTime { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Optional link to a concurrent vital sign reading taken during the symptom onset.
    /// </summary>
    public int? RelatedVitalSignReadingId { get; set; }
    public VitalSignReading? RelatedVitalSignReading { get; set; }

    public string? Description { get; set; }
    public string? ActionsTakenByCrew { get; set; }
}

