namespace AstronautHealthMonitoring.DTOs.VitalSigns;

public class VitalSignTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

