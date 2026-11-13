using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Habitat and spacecraft environmental telemetry affecting astronaut health (pressure, O2, CO2, radiation).
/// </summary>
public class EnvironmentStatus : BaseEntity
{
    public int MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cabin atmospheric pressure in kilopascals (nominal ~101.3 kPa).
    /// </summary>
    public double CabinPressureKpa { get; set; }

    /// <summary>
    /// Percentage of oxygen in the cabin atmosphere (nominal ~21%).
    /// </summary>
    public double OxygenPercentage { get; set; }

    /// <summary>
    /// Percentage of carbon dioxide in the cabin atmosphere (critical if > 0.5%).
    /// </summary>
    public double CarbonDioxidePercentage { get; set; }

    /// <summary>
    /// Ambient cabin temperature in Celsius (nominal 18 - 24 °C).
    /// </summary>
    public double CabinTemperatureCelsius { get; set; }

    /// <summary>
    /// Relative humidity percentage (nominal 40% - 60%).
    /// </summary>
    public double HumidityPercentage { get; set; }

    /// <summary>
    /// Cumulative ambient radiation rate in microsieverts per hour (uSv/h).
    /// </summary>
    public double RadiationMicroSvPerHour { get; set; }
}

