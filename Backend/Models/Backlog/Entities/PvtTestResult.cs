using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Objective cognitive readiness and alertness assessment from the Psychomotor Vigilance Task (PVT).
/// Measures sustained attention and behavioral alertness in high-risk spaceflight operations.
/// Note: Regulatory operational thresholds (e.g. lapses >= 355ms, false starts < 130ms) live in configuration, not in entities.
/// </summary>
public class PvtTestResult : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public DateTime TestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Mean reaction time across all valid visual stimuli in milliseconds.
    /// </summary>
    public double MeanResponseTimeMs { get; set; }

    /// <summary>
    /// Total count of cognitive lapses (responses exceeding the standard duration threshold, typically >= 355ms).
    /// </summary>
    public int LapsesCount { get; set; }

    /// <summary>
    /// Total count of false starts / premature responses (responses occurring before the stimulus or < 130ms).
    /// </summary>
    public int FalseStartsCount { get; set; }

    /// <summary>
    /// Duration of the test session in seconds (typically 180s for Brief PVT-B or 600s for standard PVT).
    /// </summary>
    public int TestDurationSeconds { get; set; }

    public string? Notes { get; set; }
}

