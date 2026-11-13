using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// A completed physical countermeasure exercise session logged by an astronaut.
/// </summary>
public class ExerciseLog : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public int PhysicalActivityId { get; set; }
    public PhysicalActivity PhysicalActivity { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }

    public ExerciseIntensity Intensity { get; set; } = ExerciseIntensity.Medium;
    public double CaloriesBurned { get; set; }

    public double? AverageHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }

    /// <summary>
    /// Subjective feedback from astronaut (e.g. "Mild joint soreness, completed full cycle").
    /// </summary>
    public string? AstronautNotes { get; set; }
}

