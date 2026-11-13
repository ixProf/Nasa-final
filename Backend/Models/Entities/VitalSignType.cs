using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Catalog of monitored vital signs in the MVP (Heart Rate, Blood Pressure, SpO2, Body Temperature, Blood Glucose, Respiratory Rate).
/// </summary>
public class VitalSignType : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g. "HR", "BP", "SPO2", "TEMP", "GLUCOSE", "RESP"
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty; // e.g. "bpm", "mmHg", "%", "°C", "mg/dL", "breaths/min"
    public string Description { get; set; } = string.Empty;

    // Navigation properties for MVP
    public HealthMetricGuide? HealthMetricGuide { get; set; }
    public ICollection<ReferenceRange> ReferenceRanges { get; set; } = new List<ReferenceRange>();
    public ICollection<VitalSignReading> VitalSignReadings { get; set; } = new List<VitalSignReading>();
    public ICollection<ConditionVitalSign> ConditionVitalSigns { get; set; } = new List<ConditionVitalSign>();
}
