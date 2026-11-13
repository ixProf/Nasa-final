namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

public class EvaluateSymptomsRequest
{
    public string Locale {get;set;}="en";
    public List<SymptomDetailRequest> SymptomDetails {get;set;}=[];
    public int AstronautId { get; set; }
    public List<string> SymptomCodes { get; set; } = new();
    public string Severity { get; set; } = "mild";
    public double DurationHours { get; set; }
}
public record SymptomDetailRequest
{
    public string Code {get;set;}="";
    public string Severity {get;set;}="mild";
    public double DurationHours {get;set;}
}

