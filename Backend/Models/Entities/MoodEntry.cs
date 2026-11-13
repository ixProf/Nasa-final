using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Psychological check-in entry capturing emotional state and stress levels in isolation.
/// Supports offline-sync idempotency via ClientGeneratedId.
/// </summary>
public class MoodEntry : BaseEntity
{
    /// <summary>
    /// Client-generated GUID for offline-sync idempotency.
    /// </summary>
    public Guid ClientGeneratedId { get; set; } = Guid.NewGuid();

    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    /// <summary>
    /// Exact device timestamp when mood entry was submitted in orbit.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Mood score on a scale from 1 (Very low / Depressed) to 10 (Excellent / Elevated).
    /// </summary>
    public int MoodScore { get; set; }
    /// <summary>Older stored entries use 10; new check-ins explicitly use 5.</summary>
    public int MoodScaleMaximum { get; set; } = 10;

    /// <summary>
    /// Perceived stress score from 1 (Completely relaxed) to 10 (Overwhelming stress).
    /// </summary>
    public int StressLevel { get; set; }

    /// <summary>
    /// Dominant subjective emotion (e.g. "Calm", "Anxious", "Focused", "Irritable", "Exhausted").
    /// </summary>
    public string DominantEmotion { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
