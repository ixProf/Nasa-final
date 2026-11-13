using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Services.Implementations;

public record WellnessDecision(AlertSeverity Severity,string Code,string Reason,string ReasonAr);

/// <summary>Demonstration rules from Final Edits. This evaluates patterns, never diagnoses a condition.</summary>
public class WellnessRuleService(AppDbContext db)
{
    public static WellnessDecision? EvaluateSleep(IReadOnlyList<double> latestNights)
    {
        if(latestNights.Count>=2 && latestNights.Take(2).Average()<5)
            return new(AlertSeverity.Emergency,"SLEEP","Average sleep was below 5 hours across two nights.","متوسط النوم أقل من ٥ ساعات خلال ليلتين.");
        if(latestNights.Count>=3 && latestNights.Take(3).Average()<6.5)
            return new(AlertSeverity.MedicalAttention,"SLEEP","Average sleep was below 6.5 hours across three nights.","متوسط النوم أقل من ٦٫٥ ساعات خلال ثلاث ليالٍ.");
        return null;
    }
    public static WellnessDecision? EvaluateMood(IReadOnlyList<int> latestDays)
    {
        if(latestDays.Count==0)return null;
        if(latestDays[0]==1 || latestDays.Count>=5 && latestDays.Take(5).All(score=>score<=2))
            return new(AlertSeverity.Emergency,"MOOD","Mood was 1/5, or remained at 2/5 or lower for five days.","المزاج ١ من ٥، أو ظل عند ٢ من ٥ أو أقل لمدة خمسة أيام.");
        if(latestDays[0]==3 || latestDays.Count>=2 && latestDays.Take(2).All(score=>score<=2))
            return new(AlertSeverity.MedicalAttention,"MOOD","Mood was 3/5, or remained at 2/5 or lower for two days.","المزاج ٣ من ٥، أو ظل عند ٢ من ٥ أو أقل لمدة يومين.");
        return null;
    }
    public async Task<HealthAlert?> ApplySleepAsync(SleepRecord current,CancellationToken ct)
    {
        var day=current.EndTime.Date;
        if(await db.SleepRecords.AnyAsync(s=>s.AstronautId==current.AstronautId && s.EndTime>current.EndTime,ct))return null;
        var previous=await db.SleepRecords.AsNoTracking().Where(s=>s.AstronautId==current.AstronautId && s.EndTime>=day.AddDays(-2) && s.EndTime<=current.EndTime).ToListAsync(ct);
        var nights=previous.Append(current).GroupBy(s=>s.EndTime.Date).OrderByDescending(g=>g.Key).Select(g=>g.Sum(s=>s.DurationHours)).ToList();
        // Missing nights never count as zero hours.
        var consecutive=previous.Select(s=>s.EndTime.Date).Append(day).Distinct().OrderByDescending(d=>d).ToArray();
        var count=consecutive.TakeWhile((d,i)=>d==day.AddDays(-i)).Count();
        return await ApplyAsync(current.AstronautId,current.Astronaut,EvaluateSleep(nights.Take(count).ToArray()),ct);
    }
    public async Task<HealthAlert?> ApplyMoodAsync(MoodEntry current,CancellationToken ct)
    {
        if(current.MoodScaleMaximum!=5)return null; // Legacy ten-point scores remain explicitly labelled.
        var day=current.RecordedAt.Date;
        if(await db.MoodEntries.AnyAsync(m=>m.AstronautId==current.AstronautId && m.RecordedAt>current.RecordedAt,ct))return null;
        var previous=await db.MoodEntries.AsNoTracking().Where(m=>m.AstronautId==current.AstronautId && m.MoodScaleMaximum==5 && m.RecordedAt>=day.AddDays(-4) && m.RecordedAt<day.AddDays(1)).ToListAsync(ct);
        var days=previous.Append(current).GroupBy(m=>m.RecordedAt.Date).OrderByDescending(g=>g.Key).Select(g=>g.OrderByDescending(m=>m.RecordedAt).ThenByDescending(m=>ReferenceEquals(m,current)).First()).ToArray();
        var count=days.TakeWhile((m,i)=>m.RecordedAt.Date==day.AddDays(-i)).Count();
        return await ApplyAsync(current.AstronautId,current.Astronaut,EvaluateMood(days.Take(count).Select(m=>m.MoodScore).ToArray()),ct);
    }
    private async Task<HealthAlert?> ApplyAsync(int astronautId,Astronaut astronaut,WellnessDecision? decision,CancellationToken ct)
    {
        if(decision is null)return null;
        var title=$"[{decision.Code}] Wellness review";
        var existing=await db.HealthAlerts.FirstOrDefaultAsync(a=>a.AstronautId==astronautId && a.Title==title && a.Status!=AlertStatus.Resolved,ct);
        if(existing is not null)
        {
            if(existing.Severity>=decision.Severity)return null;
            existing.Severity=decision.Severity;existing.Reason=decision.Reason;existing.Message=decision.Reason;
            existing.UpdatedAt=DateTime.UtcNow;
            var pendingAdvice=await db.Recommendations.FirstOrDefaultAsync(r=>r.AstronautId==astronautId && r.PrescribedBy==$"Automatic rule: {decision.Code}" && (r.Status==RecommendationStatus.Pending || r.Status==RecommendationStatus.Acknowledged),ct);
            if(pendingAdvice is not null)
            {
                pendingAdvice.Reason=decision.Reason;pendingAdvice.ReasonAr=decision.ReasonAr;
                pendingAdvice.Priority=RecommendationPriority.Urgent;
            }
            return existing;
        }
        var action="Review this pattern with your medical monitor. Follow the mission plan. Demonstration rule; this is not a diagnosis.";
        var alert=new HealthAlert {AstronautId=astronautId,Astronaut=astronaut,Title=title,Message=decision.Reason,Reason=decision.Reason,ActionRequired=action,Severity=decision.Severity};
        db.HealthAlerts.Add(alert);
        var ruleSource=$"Automatic rule: {decision.Code}";
        if(!await db.Recommendations.AnyAsync(r=>r.AstronautId==astronautId && r.PrescribedBy==ruleSource && (r.Status==RecommendationStatus.Pending || r.Status==RecommendationStatus.Acknowledged),ct))
            db.Recommendations.Add(new Recommendation {AstronautId=astronautId,Astronaut=astronaut,Title="Review your wellness pattern",TitleAr="مراجعة نمط العافية",Content=action,ContentAr="راجع هذا النمط مع المراقب الطبي واتبع خطة المهمة. قاعدة توضيحية وليست تشخيصًا.",Reason=decision.Reason,ReasonAr=decision.ReasonAr,PrescribedBy=ruleSource,Category=decision.Code=="SLEEP"?RecommendationCategory.Sleep:RecommendationCategory.Psychological,Priority=decision.Severity==AlertSeverity.Emergency?RecommendationPriority.Urgent:RecommendationPriority.High});
        return alert;
    }
}
