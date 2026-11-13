using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Log of an actual real-time medical or environmental emergency encountered in orbit.
/// </summary>
public class EmergencyIncident : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public int? EmergencyProcedureId { get; set; }
    public EmergencyProcedure? EmergencyProcedure { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public EmergencySeverity Severity { get; set; } = EmergencySeverity.Critical;
    public EmergencyIncidentStatus Status { get; set; } = EmergencyIncidentStatus.Active;

    public string InitialSymptoms { get; set; } = string.Empty;
    public string? ActionsTaken { get; set; }
    public string? OutcomeNotes { get; set; }
}

