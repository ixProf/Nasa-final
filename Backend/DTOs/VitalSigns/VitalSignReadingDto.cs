using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.VitalSigns;

public class VitalSignReadingDto
{
    public int Id { get; set; }
    public Guid ClientGeneratedId { get; set; }
    public int AstronautId { get; set; }
    public string AstronautFullName { get; set; } = string.Empty;

    public int VitalSignTypeId { get; set; }
    public string VitalSignTypeCode { get; set; } = string.Empty;
    public string VitalSignTypeName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public double Value { get; set; }
    public double? SecondaryValue { get; set; }

    public VitalSignStatus Status { get; set; }
    public string MeasurementMethod { get; set; } = string.Empty;
    public string SensorLocation { get; set; } = string.Empty;
    public BodyPosition BodyPosition { get; set; }
    public ActivityContext ActivityContext { get; set; }
    public DataQuality DataQuality { get; set; }

    public MealContext? MealContext { get; set; }
    public int? MinutesSinceLastMeal { get; set; }
    public string? DeviceId { get; set; }
    public string? Notes { get; set; }

    public DateTime RecordedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
