namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

public class SpaceConditionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SelfCareGuidance { get; set; } = string.Empty;
    public string WhenToContactMonitor { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public bool IsMatchable { get; set; }
    public string Notes { get; set; } = string.Empty;

    public List<ConditionSymptomDto> AssociatedSymptoms { get; set; } = new();
    public List<ConditionVitalSignDto> AssociatedVitalSigns { get; set; } = new();
}
