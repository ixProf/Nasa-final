using AstronautHealthMonitoring.Models.Common;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Dedicated radiological dosimetry tracking for an individual astronaut.
/// Kept strictly separate from momentary physiological vitals to reflect cumulative ionizing radiation exposure.
/// Tracks galactic cosmic rays (GCR) and solar particle events (SPE) against flight career exposure limits.
/// </summary>
public class RadiationDoseRecord : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    /// <summary>
    /// Ionizing radiation dose received over the specified period in millisieverts (mSv).
    /// </summary>
    public double DoseMSv { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Running total cumulative mission dose received to date in millisieverts (mSv).
    /// </summary>
    public double CumulativeMissionDoseMSv { get; set; }

    /// <summary>
    /// Originating instrument (e.g. "Crew Personal Active Dosimeter (CPAD)", "Thermoluminescent Dosimeter (TLD)", "Passive Luminescent Detector").
    /// </summary>
    public string DosimeterSource { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

