using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Record of an astronaut's attempt at an emergency simulation drill.
/// </summary>
public class SimulationAttempt : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public int SimulationScenarioId { get; set; }
    public SimulationScenario SimulationScenario { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Score achieved from 0 to 100.
    /// </summary>
    public double Score { get; set; }
    public bool Passed { get; set; }

    public string? MistakesMade { get; set; }
    public string? FeedbackNotes { get; set; }
}

