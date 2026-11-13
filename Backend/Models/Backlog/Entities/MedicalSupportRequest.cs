using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Telemedicine consultation request submitted by an astronaut to Earth ground monitors.
/// </summary>
public class MedicalSupportRequest : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public string? AssignedMedicalOfficerUserId { get; set; }

    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Medium;
    public SupportRequestStatus Status { get; set; } = SupportRequestStatus.Open;

    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<MedicalMessage> Messages { get; set; } = new List<MedicalMessage>();
}

