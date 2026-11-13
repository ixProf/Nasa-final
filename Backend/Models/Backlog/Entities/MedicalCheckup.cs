using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Clinical medical checkup or specialized telemedicine examination conducted in orbit or prior to flight.
/// Principle: The system records and displays specialized exam results; it never generates autonomous medical diagnoses.
/// </summary>
public class MedicalCheckup : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public string? MedicalOfficerUserId { get; set; }

    public DateTime ScheduledAt { get; set; }
    public DateTime? ConductedAt { get; set; }

    public CheckupType CheckupType { get; set; } = CheckupType.RoutineDaily;

    /// <summary>
    /// Specific diagnostic examination modality (Ultrasound, DXA bone density, OCT/Fundoscopy for SANS, Lab blood tests, Routine).
    /// </summary>
    public ExamType ExamType { get; set; } = ExamType.Routine;

    public CheckupStatus Status { get; set; } = CheckupStatus.Scheduled;

    /// <summary>
    /// Narrative synthesis of findings entered by a medical officer or recorded from diagnostic device.
    /// </summary>
    public string? FindingsSummary { get; set; }

    /// <summary>
    /// Quantitative or structured exam metrics summary (e.g. "Optic nerve sheath diameter 5.8mm, within acceptable variation").
    /// </summary>
    public string? ResultSummary { get; set; }

    /// <summary>
    /// Path or cloud blob URL to raw diagnostic imaging/data file (DICOM, Ultrasound scan, Fundus photo, blood lab PDF).
    /// </summary>
    public string? AttachmentReferenceUrl { get; set; }

    public string? FollowUpActions { get; set; }
}
