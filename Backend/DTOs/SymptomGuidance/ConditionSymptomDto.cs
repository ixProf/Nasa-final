namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

public class ConditionSymptomDto
{
    public int SymptomId { get; set; }
    public string SymptomCode { get; set; } = string.Empty;
    public string SymptomName { get; set; } = string.Empty;
    public bool IsRedFlag { get; set; }
    public int Weight { get; set; }
}

