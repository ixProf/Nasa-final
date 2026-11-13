namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

public class SymptomDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BodyArea { get; set; } = string.Empty;
    public bool IsRedFlag { get; set; }
}

