using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Astronaut fatigue assessment evaluating cognitive alertness and physical exhaustion.
/// </summary>
public class FatigueEntry : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Subjective overall fatigue on a 1-10 scale (1 = fully rested, 10 = completely exhausted).
    /// </summary>
    public int FatigueLevel { get; set; }

    /// <summary>
    /// Psychomotor Vigilance Task (PVT) or cognitive test score (0-100%).
    /// </summary>
    public double? CognitiveReadinessScore { get; set; }

    /// <summary>
    /// Physical muscle tiredness on a 1-10 scale.
    /// </summary>
    public int PhysicalFatigueScore { get; set; }

    public string? Symptoms { get; set; } // e.g. "Brain fog, heavy eyelids, slow reaction"
    public string? Notes { get; set; }
}

