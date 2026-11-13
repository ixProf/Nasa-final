using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.VitalSigns;

public class CreateVitalSignReadingRequest
{
    public Guid ClientGeneratedId { get; set; }
    public int VitalSignTypeId { get; set; }

    public double Value { get; set; }
    public double? SecondaryValue { get; set; }

    public string MeasurementMethod { get; set; } = string.Empty;
    public string SensorLocation { get; set; } = string.Empty;
    public BodyPosition BodyPosition { get; set; } = BodyPosition.MicrogravityFloating;
    public ActivityContext ActivityContext { get; set; } = ActivityContext.Resting;
    public DataQuality DataQuality { get; set; } = DataQuality.Valid;

    public MealContext? MealContext { get; set; }
    public int? MinutesSinceLastMeal { get; set; }
    public string? DeviceId { get; set; }
    public string? Notes { get; set; }

    public DateTime RecordedAt { get; set; }
}
