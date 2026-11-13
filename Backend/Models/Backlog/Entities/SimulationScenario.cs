using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Pre-configured medical drill or emergency simulation scenario to practice astronaut response.
/// </summary>
public class SimulationScenario : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public SimulationDifficulty Difficulty { get; set; } = SimulationDifficulty.Intermediate;

    /// <summary>
    /// Simulated patient/astronaut medical condition (e.g. "Hypercapnia during airlock cycle").
    /// </summary>
    public string TargetCondition { get; set; } = string.Empty;

    /// <summary>
    /// Actions and interventions the user is expected to execute in the scenario.
    /// </summary>
    public string ExpectedActions { get; set; } = string.Empty;

    public int TimeLimitSeconds { get; set; }

    // Navigation properties
    public ICollection<SimulationAttempt> Attempts { get; set; } = new List<SimulationAttempt>();
}

