using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Interfaces;

namespace AstronautHealthMonitoring.Services.Implementations;

/// <summary>
/// Pure stateless evaluator implementing the 3-tier vital sign status rule.
/// Zero hard-coded numbers; all threshold evaluation is dynamically driven by the passed ReferenceRange.
/// </summary>
public class VitalSignStatusEvaluator : IVitalSignStatusEvaluator
{
    public VitalSignStatus Evaluate(ReferenceRange range, double value, double? secondaryValue = null)
    {
        ArgumentNullException.ThrowIfNull(range);

        bool isPaired = range.MinSecondaryNormal.HasValue
                     || range.MaxSecondaryNormal.HasValue
                     || range.SecondaryEmergencyBelow.HasValue
                     || range.SecondaryEmergencyAbove.HasValue;

        if (isPaired && !secondaryValue.HasValue)
        {
            throw new ArgumentException("Secondary value is required for paired reference ranges.", nameof(secondaryValue));
        }

        // 1. Emergency FIRST:
        // Value is strictly below EmergencyBelow or strictly above EmergencyAbove (null = no limit),
        // or, for paired metrics, secondaryValue breaches SecondaryEmergencyBelow/Above.
        // Boundaries: exactly EmergencyBelow or EmergencyAbove is NOT Emergency.
        if (range.EmergencyBelow.HasValue && value < range.EmergencyBelow.Value)
        {
            return VitalSignStatus.Emergency;
        }

        if (range.EmergencyAbove.HasValue && value > range.EmergencyAbove.Value)
        {
            return VitalSignStatus.Emergency;
        }

        if (isPaired && secondaryValue.HasValue)
        {
            if (range.SecondaryEmergencyBelow.HasValue && secondaryValue.Value < range.SecondaryEmergencyBelow.Value)
            {
                return VitalSignStatus.Emergency;
            }

            if (range.SecondaryEmergencyAbove.HasValue && secondaryValue.Value > range.SecondaryEmergencyAbove.Value)
            {
                return VitalSignStatus.Emergency;
            }
        }

        // 2. Routine:
        // Value is within [MinNormal, MaxNormal] inclusive.
        // For paired metrics, BOTH primary and secondary must be within their normal ranges.
        // Boundaries: exactly MinNormal or MaxNormal is Routine.
        bool isPrimaryRoutine = value >= range.MinNormal && value <= range.MaxNormal;

        bool isSecondaryRoutine = !isPaired ||
            (secondaryValue.HasValue &&
             (!range.MinSecondaryNormal.HasValue || secondaryValue.Value >= range.MinSecondaryNormal.Value) &&
             (!range.MaxSecondaryNormal.HasValue || secondaryValue.Value <= range.MaxSecondaryNormal.Value));

        if (isPrimaryRoutine && isSecondaryRoutine)
        {
            return VitalSignStatus.Routine;
        }

        // 3. Otherwise: MedicalAttention
        return VitalSignStatus.MedicalAttention;
    }
}
