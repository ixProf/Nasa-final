using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// A recorded health measurement from wearable sensors or astronaut manual entry.
/// Supports offline synchronization and rich physiological context.
/// </summary>
public class VitalSignReading : BaseEntity
{
    /// <summary>
    /// Client-side generated GUID for offline-sync idempotency.
    /// Used as a unique key to ignore duplicates during batch sync.
    /// </summary>
    public Guid ClientGeneratedId { get; set; } = Guid.NewGuid();

    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public int VitalSignTypeId { get; set; }
    public VitalSignType VitalSignType { get; set; } = null!;

    /// <summary>
    /// Exact device timestamp when the measurement was captured in orbit.
    /// Distinct from BaseEntity.CreatedAt (database ingestion time).
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Primary measured value (e.g., HR in bpm, SpO2 in %, Systolic in mmHg, Temp in °C, Glucose in mg/dL).
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Optional secondary value for paired metrics (e.g., Diastolic blood pressure when Systolic is in Value).
    /// </summary>
    public double? SecondaryValue { get; set; }

    /// <summary>
    /// Evaluated status against reference ranges (Routine, MedicalAttention, Emergency).
    /// </summary>
    public VitalSignStatus Status { get; set; } = VitalSignStatus.Routine;

    public string MeasurementMethod { get; set; } = string.Empty;
    public string SensorLocation { get; set; } = string.Empty;
    public BodyPosition BodyPosition { get; set; } = BodyPosition.MicrogravityFloating;
    public ActivityContext ActivityContext { get; set; } = ActivityContext.Resting;
    public DataQuality DataQuality { get; set; } = DataQuality.Valid;

    // Metabolic context for blood glucose
    public MealContext? MealContext { get; set; }
    public int? MinutesSinceLastMeal { get; set; }

    public string? DeviceId { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<HealthAlert> HealthAlerts { get; set; } = new List<HealthAlert>();
}
