namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

/// <summary>
/// Possible related health topic matching crew-reported symptoms.
/// Non-diagnostic guidance using 'may be associated with' wording.
/// </summary>
public class ConditionMatchResultDto
{
    public string HazardCode {get;set;} = "";
    public int FollowUpLevel {get;set;}=1;
    public decimal MatchPercentage => AssociationIndex;
    public decimal? ProbabilityPercent {get;set;}
    public string ProbabilityStatus {get;set;}="model_not_configured";
    public List<string> FollowUpReasons {get;set;}=[];
    public decimal AssociationIndex {get;set;}
    public string Strength {get;set;} = "weak";
    public string Explanation {get;set;} = "";
    public string ExplanationAr {get;set;} = "";
    public List<string> MatchedSymptomCodes {get;set;}=[];
    public AssociationBreakdown? Breakdown {get;set;}
    public string ConditionCode { get; set; } = string.Empty;
    public string ConditionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SelfCareGuidance { get; set; } = string.Empty;
    public string WhenToContactMonitor { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public List<string> MatchedSymptoms { get; set; } = new();
    public bool HasRedFlag { get; set; }
}
