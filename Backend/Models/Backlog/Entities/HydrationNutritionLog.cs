using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Daily metabolic monitoring tracking hydration volume, nutritional intake, and mass changes.
/// Crucial for preventing fluid deficit, negative energy balance, and accelerated muscle loss in space.
/// </summary>
public class HydrationNutritionLog : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public DateTime LogDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Total fluid consumption logged for the day in milliliters (nominal target >= 2000 - 2500 mL).
    /// </summary>
    public double FluidIntakeMl { get; set; }

    /// <summary>
    /// Total caloric energy consumed in kilocalories (kcal).
    /// </summary>
    public double EnergyIntakeKcal { get; set; }

    /// <summary>
    /// Body mass in kilograms, measured via microgravity inertial mass instruments (e.g. SLAMMD or Space Mass Measurement Device).
    /// Nullable because mass is measured periodically (e.g. weekly) rather than continuously.
    /// </summary>
    public double? BodyMassKg { get; set; }

    public string? Notes { get; set; }
}

