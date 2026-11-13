using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Core profile for an astronaut in the competition MVP.
/// Contains physiological metadata, searchable operational identifiers, and navigations to MVP health telemetry.
/// </summary>
public class Astronaut : BaseEntity
{
    // Identity link for Auth (Phase 4)
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Operational code for quick monitor search (e.g., "AST-042", "CDR-EVA-1").
    /// </summary>
    public string AstronautCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Pre-computed/stored full name for fast and indexed search across monitor dashboards.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public string? EmergencyContactInfo { get; set; }

    /// <summary>
    /// Current mission assignment for the astronaut (nullable).
    /// </summary>
    public int? MissionId { get; set; } // Foreign Key
    public Mission? Mission { get; set; } // Navigation

    /// <summary>
    /// Indicates whether the astronaut's flight profile is active.
    /// Deactivated astronauts have their Identity account locked and refresh tokens revoked.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Active MVP telemetry and workflow collections
    public ICollection<VitalSignReading> VitalSignReadings { get; set; } = new List<VitalSignReading>();
    public ICollection<SleepRecord> SleepRecords { get; set; } = new List<SleepRecord>();
    public ICollection<MoodEntry> MoodEntries { get; set; } = new List<MoodEntry>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public ICollection<HealthAlert> HealthAlerts { get; set; } = new List<HealthAlert>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
