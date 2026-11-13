using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Emergency;

public class EmergencyProcedureDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TargetCondition { get; set; } = string.Empty;
    public EmergencySeverity Severity { get; set; }
    public string ImmediateAction { get; set; } = string.Empty;
    public string StepByStepInstructions { get; set; } = string.Empty;
    public string RequiredEquipment { get; set; } = string.Empty;
    public string? GuidanceNotes { get; set; }
}

public class TriggerEmergencyAlertRequest
{
    public int AstronautId { get; set; }
    public int EmergencyProcedureId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ActionRequired { get; set; } = string.Empty;
}

