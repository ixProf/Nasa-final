using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Alerts;

public class HealthAlertDto
{
    public int Id { get; set; }
    public int AstronautId { get; set; }
    public string AstronautFullName { get; set; } = string.Empty;

    public int? VitalSignReadingId { get; set; }
    public int? VitalSignTypeId { get; set; }
    public string? VitalSignTypeCode { get; set; }

    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ActionRequired { get; set; } = string.Empty;

    public DateTime TriggeredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? ActionTaken { get; set; }
    public bool IsDelayed { get; set; }

    public int? EmergencyProcedureId { get; set; }
    public string? EmergencyProcedureCode { get; set; }
    public string? EmergencyProcedureTitle { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
