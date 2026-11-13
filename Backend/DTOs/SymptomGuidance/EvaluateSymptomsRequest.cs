namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

public class EvaluateSymptomsRequest
{
    public int AstronautId { get; set; }
    public List<string> SymptomCodes { get; set; } = new();
    public string Severity { get; set; } = "mild";
    public double DurationHours { get; set; }
}

