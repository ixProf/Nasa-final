using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Evaluates raw vital sign measurements against operational reference ranges
/// to determine the 3-tier clinical severity status (Routine, MedicalAttention, Emergency).
/// Deterministic and stateless evaluation; does not assign diagnoses.
/// </summary>
public interface IVitalSignStatusEvaluator
{
    /// <summary>
    /// Evaluates a primary and optional secondary measurement against the specified reference range.
    /// </summary>
    /// <param name="range">The reference range containing normal and emergency thresholds.</param>
    /// <param name="value">The primary metric value (e.g., HR, Systolic BP, SpO2, Temp, Glucose, RESP).</param>
    /// <param name="secondaryValue">The secondary metric value for paired metrics (e.g., Diastolic BP).</param>
    /// <returns>The evaluated status: Routine, MedicalAttention, or Emergency.</returns>
    /// <exception cref="ArgumentNullException">Thrown when range is null.</exception>
    /// <exception cref="ArgumentException">Thrown when secondaryValue is null for a paired metric.</exception>
    VitalSignStatus Evaluate(ReferenceRange range, double value, double? secondaryValue = null);
}
