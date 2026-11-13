namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

/// <summary>
/// Possible related health topic matching crew-reported symptoms.
/// Non-diagnostic guidance using 'may be associated with' wording.
/// </summary>
public class ConditionMatchResultDto
{
    public string ConditionCode { get; set; } = string.Empty;
    public string ConditionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SelfCareGuidance { get; set; } = string.Empty;
    public string WhenToContactMonitor { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public List<string> MatchedSymptoms { get; set; } = new();
    public bool HasRedFlag { get; set; }
}
