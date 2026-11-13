using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Alerts;

/// <summary>
/// Payload for raising a manual health safety alert from an astronaut terminal or monitor console.
/// </summary>
public class TriggerManualAlertRequest
{
    /// <summary>
    /// Target astronaut ID. Ignored for astronaut callers (automatically scoped to self).
    /// </summary>
    public int AstronautId { get; set; }

    /// <summary>
    /// Alert severity level: MedicalAttention or Emergency.
    /// </summary>
    public AlertSeverity Severity { get; set; } = AlertSeverity.MedicalAttention;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public string? ActionRequired { get; set; }

    public int? EmergencyProcedureId { get; set; }
}

