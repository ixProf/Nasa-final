namespace AstronautHealthMonitoring.DTOs.SymptomGuidance;
public record SymptomFindingDto(string Code,string Name,string Severity,double DurationHours,int Level,List<string> Reasons);

public record AssociationBreakdown(decimal MatchedWeight,decimal TotalWeight,decimal Coverage,decimal Evidence,
    decimal PhaseGate,int VitalBoost,int ContextBoost,int Cap,decimal UncappedIndex,string PhaseState);
public record HazardScoreDto(string Hazard,decimal Index,string Strength,string Explanation,string ExplanationAr,string ConditionCode);
public record KnowledgeCardDto(string Code,string Name,string Explanation,string ExplanationAr);
public record RadiationAssessmentDto(string Status,string Explanation,string ExplanationAr);

// Created by the server from trusted mission configuration and actual records, never unchecked client flags.
public record AssociationContext
{
    public int? MissionDay {get;init;}
    public double? HoursSinceGravityTransition {get;init;}
    public string? GravityTransitionKind {get;init;}
    public int CommsDelaySeconds {get;init;}
    public bool RadiationEventActive {get;init;}
    public bool Co2EventActive {get;init;}
    public bool SleepBelowBaselineThreeNights {get;init;}
    public bool MoodLowTwoDays {get;init;}
}
