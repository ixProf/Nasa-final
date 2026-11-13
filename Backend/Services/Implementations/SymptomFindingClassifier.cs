using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Services.Implementations;

public static class SymptomFindingClassifier
{
    public static void Apply(SymptomAssessmentResponseDto result,EvaluateSymptomsRequest request,
        IReadOnlyList<Symptom> symptoms,IReadOnlyList<SpaceCondition> conditions,IReadOnlyList<VitalSignReading> readings,double? sleepHours)
    {
        result.SymptomFindings=symptoms.Select(symptom=>{
            var detail=request.SymptomDetails?.FirstOrDefault(d=>d.Code.Trim().Equals(symptom.Code,StringComparison.OrdinalIgnoreCase));
            // Legacy global fields are used only for older clients without individual entries.
            var severity=detail?.Severity ?? (request.SymptomDetails?.Count>0?"mild":request.Severity);
            var duration=detail?.DurationHours ?? (request.SymptomDetails?.Count>0?0:request.DurationHours);
            var finding=SymptomAssessmentRules.Evaluate(new(){Severity=severity,DurationHours=duration,SymptomCodes=[symptom.Code]},[symptom],readings,sleepHours);
            if(symptom.Code is "PALPITATIONS" or "NECK_FACE_SWELLING")finding.Level=Math.Max(2,finding.Level);
            return new SymptomFindingDto(symptom.Code,symptom.Name,severity,duration,finding.Level,finding.Reasons);
        }).ToList();
        foreach(var match in result.ConditionMatches)
        {
            var related=result.SymptomFindings.Where(s=>match.MatchedSymptomCodes.Contains(s.Code)).ToArray();
            match.FollowUpLevel=related.Select(s=>s.Level).DefaultIfEmpty(1).Max();
            match.FollowUpReasons=related.Where(s=>s.Level>1).Select(s=>$"{s.Code}: {s.Severity}, {s.DurationHours:0.#} h").ToList();
            var condition=conditions.Single(c=>c.Code==match.ConditionCode);
            var relevant=readings.Where(r=>condition.ConditionVitalSigns.Any(v=>v.VitalSignTypeId==r.VitalSignTypeId)).ToArray();
            if(relevant.Any(r=>r.Status==VitalSignStatus.Emergency))
            {
                match.FollowUpLevel=3;
                match.FollowUpReasons.Add("A related recent vital reading has emergency status.");
            }
            else if(relevant.Any(r=>r.Status==VitalSignStatus.MedicalAttention))match.FollowUpLevel=Math.Max(2,match.FollowUpLevel);
        }
        result.Level=Math.Max(result.SymptomFindings.Select(s=>s.Level).DefaultIfEmpty(1).Max(),result.ConditionMatches.Select(c=>c.FollowUpLevel).DefaultIfEmpty(1).Max());
        if(result.Radiation.Status=="rule_triggered")result.Level=Math.Max(2,result.Level);
        result.ShouldEscalate=result.Level==3;
        // Critical findings precede larger match percentages; numbers never replace urgency.
        result.ConditionMatches=result.ConditionMatches.OrderByDescending(c=>c.FollowUpLevel).ThenByDescending(c=>c.AssociationIndex).ThenBy(c=>c.ConditionCode,StringComparer.Ordinal).ToList();
    }
}
