namespace AstronautHealthMonitoring.DTOs.Guides;

public class HealthMetricGuideDto
{
    public int Id { get; set; }
    public int VitalSignTypeId { get; set; }
    public string VitalSignTypeCode { get; set; } = string.Empty;

    public string WhatIsIt { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string HowToMeasure { get; set; } = string.Empty;
    public string InterpretationGuidance { get; set; } = string.Empty;
    public string ActionGuidance { get; set; } = string.Empty;
}

public class UpdateHealthMetricGuideRequest
{
    public string WhatIsIt { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string HowToMeasure { get; set; } = string.Empty;
    public string InterpretationGuidance { get; set; } = string.Empty;
    public string ActionGuidance { get; set; } = string.Empty;
}

