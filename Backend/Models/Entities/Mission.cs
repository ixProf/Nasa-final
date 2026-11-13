using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Operational space exploration mission in the MVP.
/// </summary>
public class Mission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public MissionStatus Status { get; set; } = MissionStatus.Scheduled;

    // Navigation property for assigned astronauts in MVP
    public ICollection<Astronaut> Astronauts { get; set; } = new List<Astronaut>();
}
