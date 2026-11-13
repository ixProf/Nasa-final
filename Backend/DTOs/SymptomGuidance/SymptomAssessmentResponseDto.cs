namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;

/// <summary>
/// Result of symptom assessment, including non-diagnostic educational guidance and emergency escalation flags.
/// </summary>
public class SymptomAssessmentResponseDto
{
    public string EngineVersion {get;set;}="2.0-demo";
    public List<SymptomFindingDto> SymptomFindings {get;set;}=[];
    public string PercentageMeaning {get;set;}="Symptom-pattern match; not a calibrated probability of disease.";
    public List<ConditionMatchResultDto> ConditionMatches {get;set;}=[];
    public List<HazardScoreDto> HazardScores {get;set;}=[];
    public List<KnowledgeCardDto> KnowledgeOnly {get;set;}=[];
    public string GuidanceMode {get;set;}="REAL_TIME";
    public int CommsDelaySeconds {get;set;}
    public List<string> ContextWarnings {get;set;}=[];
    public RadiationAssessmentDto Radiation {get;set;}=new("withheld","Cannot be estimated from symptoms alone.","لا يمكن تقدير الإشعاع من الأعراض وحدها.");
    public string Footer {get;set;}="Guidance index based on written rules — not a diagnosis and not a medical probability. Contact your medical officer if you are concerned.";
    public string FooterAr {get;set;}="مؤشر استرشادي مبني على قواعد مكتوبة، وليس تشخيصًا ولا احتمالًا طبيًا. تواصل مع المسؤول الطبي عند القلق.";
    public int Level { get; set; } = 1;
    public List<string> Reasons { get; set; } = new();
    public List<string> RedFlags { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public string ThresholdLabel { get; set; } = "Demo thresholds – to be reviewed by the mission medical team";
    public string? AlertDeliveryError { get; set; }
    /// <summary>
    /// Explicit non-diagnostic disclaimer required for astronaut clinical safety.
    /// </summary>
    public string Disclaimer { get; set; } = "Guidance only, not a clinical diagnosis. Consult medical officer or flight surgeon.";

    /// <summary>
    /// Whether the reported signs warrant immediate flight monitor escalation.
    /// </summary>
    public bool ShouldEscalate { get; set; }

    /// <summary>
    /// Reason explaining why escalation was triggered.
    /// </summary>
    public string? EscalationReason { get; set; }

    /// <summary>
    /// Identifier of the automatically created health alert if emergency escalation was triggered.
    /// </summary>
    public int? TriggeredAlertId { get; set; }

    /// <summary>
    /// Ranked list of potential matching space health conditions based on reported symptoms.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public List<ConditionMatchResultDto> Matches { get; set; } = new();
}

