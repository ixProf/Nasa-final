using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Dashboard;

public class HealthTrendDto
{
    public int VitalSignTypeId { get; set; }
    public string VitalSignTypeCode { get; set; } = string.Empty;
    public string VitalSignTypeName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public List<HealthTrendDataPointDto> DataPoints { get; set; } = new();
}

public class HealthTrendDataPointDto
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public double? SecondaryValue { get; set; }
    public VitalSignStatus Status { get; set; }
}

