using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Reference values used by the rule-based alert engine to evaluate readings.
/// Supports both scalar metrics (Heart Rate, SpO2) and paired metrics (Blood Pressure Systolic/Diastolic).
/// A range is unreviewed until operational staff or ground monitors formally sign off.
/// </summary>
/// <remarks>
/// Status determination rule:
/// - Routine if the value is within [MinNormal, MaxNormal] (for paired metrics, BOTH values must be within their normal ranges).
/// - Emergency if the value is below EmergencyBelow or above EmergencyAbove (either may be null; for paired metrics check each value against its own secondary pair).
/// - Otherwise MedicalAttention.
/// </remarks>
public class ReferenceRange : BaseEntity
{
    public int VitalSignTypeId { get; set; }
    public VitalSignType VitalSignType { get; set; } = null!;

    // Primary Metric Thresholds (e.g. Heart Rate, SpO2, Systolic BP)
    public double MinNormal { get; set; }
    public double MaxNormal { get; set; }

    // Emergency thresholds (single metric)
    public double? EmergencyBelow { get; set; }
    public double? EmergencyAbove { get; set; }

    // Secondary Metric Thresholds for Paired Metrics (e.g. Diastolic BP)
    public double? MinSecondaryNormal { get; set; }
    public double? MaxSecondaryNormal { get; set; }

    // Emergency thresholds for secondary metric
    public double? SecondaryEmergencyBelow { get; set; }
    public double? SecondaryEmergencyAbove { get; set; }

    public string? ApplicableGender { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    /// <summary>
    /// Source document or standard (e.g. "NASA-STD-3001", "ESA Medical Ops", "Earth General Clinical").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    public ReferenceRangeScope Scope { get; set; } = ReferenceRangeScope.EarthGeneral;

    /// <summary>
    /// Indicates whether support staff have reviewed and certified this threshold for the mission.
    /// </summary>
    public bool IsMissionApproved { get; set; } = false;

    /// <summary>
    /// Name or ID of reviewer who signed off. Nullable: unreviewed until certified.
    /// </summary>
    public string? ReviewedBy { get; set; }

    /// <summary>
    /// Timestamp of review. Nullable: unreviewed until certified.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
