using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

public class ConditionVitalSignDto
{
    public int VitalSignTypeId { get; set; }
    public string VitalSignTypeCode { get; set; } = string.Empty;
    public string VitalSignTypeName { get; set; } = string.Empty;
    public VitalSignDirection Direction { get; set; }
    public string? ContextNote { get; set; }
}

