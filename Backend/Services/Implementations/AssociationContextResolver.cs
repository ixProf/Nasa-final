using System.Globalization;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Services.Implementations;

public static class AssociationContextResolver
{
    public static async Task<AssociationContext> ResolveAsync(AppDbContext db,int astronautId,IConfiguration? config,DateTime now,CancellationToken ct)
    {
        var astronaut=await db.Astronauts.AsNoTracking().Include(a=>a.Mission).SingleAsync(a=>a.Id==astronautId,ct);
        // Optional recorded mission context is administrator-owned configuration; never client booleans.
        var mission=config?.GetSection($"SymptomAssessment:Missions:{astronaut.MissionId}");
        DateTimeOffset? ReadTime(string key)=>DateTimeOffset.TryParse(mission?[key],CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var time)?time:null;
        var transition=ReadTime("GravityTransitionAtUtc");
        var radiation=ReadTime("RadiationEventAtUtc");var co2=ReadTime("Co2EventAtUtc");
        bool Recent(DateTimeOffset? time,int hours)=>time.HasValue && time.Value.UtcDateTime<=now && time.Value.UtcDateTime>=now.AddHours(-hours);
        var sleeps=await db.SleepRecords.AsNoTracking().Where(s=>s.AstronautId==astronautId && s.EndTime>=now.Date.AddDays(-3) && s.EndTime<=now).ToListAsync(ct);
        var nights=sleeps.GroupBy(s=>s.EndTime.Date).OrderByDescending(g=>g.Key).Take(3).ToArray();
        var baseline=double.TryParse(config?[$"SymptomAssessment:SleepBaselines:{astronautId}"],CultureInfo.InvariantCulture,out var hours)?hours:(double?)null;
        var lowSleep=nights.Length==3 && (now.Date-nights[0].Key).TotalDays<=1 && nights[2].Key==nights[0].Key.AddDays(-2) && nights.All(g=>g.Sum(s=>s.DurationHours)<5 || baseline.HasValue && g.Sum(s=>s.DurationHours)<baseline.Value-1);
        var moods=await db.MoodEntries.AsNoTracking().Where(m=>m.AstronautId==astronautId && m.RecordedAt>=now.Date.AddDays(-2) && m.RecordedAt<=now).ToListAsync(ct);
        var days=moods.GroupBy(m=>m.RecordedAt.Date).OrderByDescending(g=>g.Key).Take(2).ToArray();
        var lowMood=days.Length==2 && (now.Date-days[0].Key).TotalDays<=1 && days[1].Key==days[0].Key.AddDays(-1) && days.All(g=>{var m=g.OrderByDescending(m=>m.RecordedAt).ThenByDescending(m=>m.Id).First();return m.MoodScaleMaximum==5 && m.MoodScore<=2;});
        return new(){MissionDay=astronaut.Mission is not null && astronaut.Mission.StartDate<=now?(now.Date-astronaut.Mission.StartDate.Date).Days+1:null,
            HoursSinceGravityTransition=transition.HasValue && transition.Value.UtcDateTime<=now?(now-transition.Value.UtcDateTime).TotalHours:null,
            GravityTransitionKind=mission?["GravityTransitionKind"],CommsDelaySeconds=int.TryParse(mission?["CommsDelaySeconds"],out var delay)?Math.Clamp(delay,0,86400):0,
            RadiationEventActive=Recent(radiation,48),Co2EventActive=Recent(co2,24),SleepBelowBaselineThreeNights=lowSleep,MoodLowTwoDays=lowMood};
    }
}
