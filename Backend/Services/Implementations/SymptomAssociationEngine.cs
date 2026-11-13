using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Services.Implementations;

/// <summary>Report v2 demo rules. Association strength is separate from medical escalation.</summary>
public static class SymptomAssociationEngine
{
    public static string Strength(decimal value)=>value<30?"weak":value<=60?"moderate":"strong";
    public static void Apply(SymptomAssessmentResponseDto result,IReadOnlyCollection<string> symptomCodes,
        IReadOnlyList<SpaceCondition> conditions,IReadOnlyList<VitalSignReading> latest,AssociationContext context)
    {
        var codes=symptomCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        result.ConditionMatches=[];result.KnowledgeOnly=[];
        foreach(var condition in conditions.Where(c=>c.IsMatchable && c.ConfidenceCap.HasValue))
        {
            var links=condition.ConditionSymptoms.Where(s=>s.Weight>0).ToArray();
            var matched=links.Where(s=>codes.Contains(s.Symptom.Code)).ToArray();
            if(matched.Length==0)continue;
            var total=links.Sum(s=>s.Weight);if(total<=0)continue;
            var weight=matched.Sum(s=>s.Weight);var coverage=weight/total;var evidence=Math.Min(1m,weight);
            var gate=1m;var phase="not_applicable";
            if(condition.Code=="SPACE_MOTION_SICKNESS")
            {
                var open=context.MissionDay is >=1 and <=5 || context.HoursSinceGravityTransition is >=0 and <=120;
                var known=context.MissionDay.HasValue || context.HoursSinceGravityTransition.HasValue;
                phase=open?"inside_window":known?"outside_window":"unknown";gate=open?1m:0.5m;
            }
            if(condition.Code=="ORTHOSTATIC_INTOLERANCE")
            {
                var open=context.GravityTransitionKind is "landing" or "arrival" && context.HoursSinceGravityTransition is >=0 and <=72;
                phase=open?"inside_window":context.GravityTransitionKind is null?"unknown":"outside_window";gate=open?1m:0.5m;
            }
            var vital=condition.ConditionVitalSigns.SelectMany(link=>latest.Where(r=>r.VitalSignTypeId==link.VitalSignTypeId))
                .Select(r=>r.Status==VitalSignStatus.Emergency?25:r.Status==VitalSignStatus.MedicalAttention?15:0).DefaultIfEmpty().Max();
            var boost=condition.Code switch {"SLEEP_DEFICIENCY" when context.SleepBelowBaselineThreeNights=>10,"STRESS_ISOLATION" when context.MoodLowTwoDays=>10,"CLOSED_ENVIRONMENT_AIR_QUALITY" when context.Co2EventActive=>20,_=>0};
            var uncapped=coverage*evidence*100m*gate+vital+boost;
            var cap=Math.Clamp(condition.ConfidenceCap!.Value,0,99);var index=Math.Round(Math.Min(cap,uncapped),2,MidpointRounding.AwayFromZero);
            var en=phase=="outside_window"?"Late onset is atypical for this pattern; consider other causes.":phase=="unknown"?"Phase timing is unavailable; a conservative phase factor is applied.":"Computed from matched symptom weights and the recorded context.";
            var ar=phase=="outside_window"?"البداية المتأخرة غير نمطية لهذا النمط؛ فكّر في أسباب أخرى.":phase=="unknown"?"توقيت المرحلة غير متاح؛ طُبّق معامل مرحلة تحفظي.":"حُسب من أوزان الأعراض المتطابقة والسياق المسجّل.";
            result.ConditionMatches.Add(new(){ConditionCode=condition.Code,ConditionName=condition.Name,HazardCode=condition.HazardCode,AssociationIndex=index,Strength=Strength(index),Description=condition.Description,SelfCareGuidance=condition.SelfCareGuidance,WhenToContactMonitor=condition.WhenToContactMonitor,SourceReference=condition.SourceReference,MatchedSymptoms=matched.Select(m=>m.Symptom.Name).ToList(),MatchedSymptomCodes=matched.Select(m=>m.Symptom.Code).ToList(),HasRedFlag=matched.Any(m=>m.Symptom.IsRedFlag),Explanation=en,ExplanationAr=ar,Breakdown=new(weight,total,coverage,evidence,gate,vital,boost,cap,uncapped,phase)});
        }
        result.ConditionMatches=result.ConditionMatches.OrderByDescending(m=>m.AssociationIndex).ThenBy(m=>m.ConditionCode,StringComparer.Ordinal).ToList();
        result.Matches=result.ConditionMatches;
        result.HazardScores=result.ConditionMatches.GroupBy(m=>m.HazardCode).Select(g=>g.First()).Select(m=>new HazardScoreDto(m.HazardCode,m.AssociationIndex,m.Strength,m.Explanation,m.ExplanationAr,m.ConditionCode)).OrderByDescending(h=>h.Index).ThenBy(h=>h.Hazard,StringComparer.Ordinal).ToList();
        var knowledge=new HashSet<string>();
        if(codes.Contains("NECK_FACE_SWELLING"))knowledge.Add("VENOUS_THROMBOEMBOLISM");
        if(codes.Contains("FLANK_PAIN_HEMATURIA")){knowledge.Add("RENAL_STONE_RISK");knowledge.Add("BONE_LOSS");}
        if(codes.Contains("MUSCULOSKELETAL_PAIN"))knowledge.Add("MUSCLE_ATROPHY");
        foreach(var condition in conditions.Where(c=>!c.IsMatchable && knowledge.Contains(c.Code)))
            result.KnowledgeOnly.Add(new(condition.Code,condition.Name,"Cannot be assessed from symptoms; requires a specialized examination within the mission plan.","لا يمكن تقييمها من الأعراض؛ تحتاج فحصًا متخصصًا ضمن خطة المهمة."));
        if(codes.Overlaps(["PALPITATIONS","NECK_FACE_SWELLING"]))result.Level=Math.Max(2,result.Level);
        var radiation=context.RadiationEventActive && codes.Contains("NAUSEA_VOMITING") && codes.Overlaps(["FATIGUE","HEADACHE","SKIN_SYMPTOMS","LOSS_OF_APPETITE"]);
        if(radiation){result.Level=Math.Max(2,result.Level);result.Radiation=new("rule_triggered","A recorded radiation event and matching symptoms triggered this rule. Follow the mission shelter / radiation protocol now.","فُعّلت القاعدة لوجود حدث إشعاعي مسجّل وأعراض مرتبطة به. اتبع بروتوكول الاحتماء الخاص بالمهمة الآن.");}
        result.CommsDelaySeconds=context.CommsDelaySeconds;result.GuidanceMode=context.CommsDelaySeconds>=60?"DELAYED":"REAL_TIME";
        if(!context.MissionDay.HasValue)result.ContextWarnings.Add("MISSION_PHASE_UNAVAILABLE");
        result.ThresholdLabel="Demo thresholds — pending medical review";
    }
}
