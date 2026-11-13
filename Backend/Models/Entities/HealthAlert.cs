using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Rule-based health alert triggered when telemetry breaches safety thresholds.
/// System is deterministic and never diagnoses diseases.
/// Broadcasted via SignalR to astronaut dashboard and Monitor staff.
/// </summary>
public class HealthAlert : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    /// <summary>
    /// Foreign key to the specific vital reading that triggered this alert (if applicable).
    /// </summary>
    public int? VitalSignReadingId { get; set; }
    public VitalSignReading? VitalSignReading { get; set; }

    /// <summary>
    /// Foreign key to the recommended emergency protocol (if alert escalates to Emergency severity).
    /// </summary>
    public int? EmergencyProcedureId { get; set; }
    public EmergencyProcedure? EmergencyProcedure { get; set; }

    /// <summary>
    /// Unified 3-tier severity level: Routine, MedicalAttention, Emergency.
    /// </summary>
    public AlertSeverity Severity { get; set; } = AlertSeverity.MedicalAttention;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Rule-based explanation of why this alert fired (e.g., "Heart rate exceeded 130 bpm during resting context").
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Actionable instructions on what to check or do right now.
    /// </summary>
    public string ActionRequired { get; set; } = string.Empty;

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Identifier or username of the Monitor who acknowledged this alert.
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Actions or countermeasures logged by the Monitor when acknowledging.
    /// </summary>
    public string? ActionTaken { get; set; }

    public AlertStatus Status { get; set; } = AlertStatus.Active;

    /// <summary>
    /// Indicates whether this alert was raised from a late-synced / delayed offline reading packet.
    /// </summary>
    public bool IsDelayed { get; set; } = false;
}
