using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Represents the astronaut's operational assignment to a mission, tracking role and flight duty status.
/// </summary>
public class CrewMember : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public int MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public CrewRole Role { get; set; } = CrewRole.MissionSpecialist;
    public FlightStatus FlightStatus { get; set; } = FlightStatus.Assigned;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

