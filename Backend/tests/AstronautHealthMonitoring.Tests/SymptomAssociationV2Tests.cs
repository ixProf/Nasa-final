using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AstronautHealthMonitoring.Tests;

public class SymptomAssociationV2Tests : IDisposable
{
    private readonly AppDbContext db=new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    public SymptomAssociationV2Tests()=>db.Database.EnsureCreated();
    public void Dispose()=>db.Dispose();
    private SymptomAssessmentResponseDto Evaluate(string[] codes,AssociationContext? context=null)
    {
        var symptoms=db.Symptoms.Where(s=>codes.Contains(s.Code)).ToArray();
        var response=SymptomAssessmentRules.Evaluate(new(){SymptomCodes=codes.ToList()},symptoms,[],null);
        var conditions=db.SpaceConditions.Include(c=>c.ConditionSymptoms).ThenInclude(s=>s.Symptom).Include(c=>c.ConditionVitalSigns).ToList();
        SymptomAssociationEngine.Apply(response,codes,conditions,[],context??new(){MissionDay=2});return response;
    }
    [Fact] public void Catalog_ContainsAllSymptomsAndRedFlags_WithFractionalWeights()
    {
        Assert.Equal(32,db.Symptoms.Count());Assert.Equal(8,db.Symptoms.Count(s=>s.IsRedFlag));
        Assert.Equal(10,db.SpaceConditions.Count(c=>c.IsMatchable));
        Assert.Equal(0.30m,db.ConditionSymptoms.Include(s=>s.Symptom).Single(s=>s.Symptom.Code=="DIFFICULTY_BREATHING").Weight);
    }
    [Fact] public void SleepExample_MatchesExactFormula_NotSymptomCount()
    {
        var r=Evaluate(["FATIGUE","DIFFICULTY_CONCENTRATING","POOR_SLEEP","DAYTIME_SLEEPINESS"]);
        Assert.Equal(78.57m,r.ConditionMatches.Single(c=>c.ConditionCode=="SLEEP_DEFICIENCY").AssociationIndex);
        Assert.Equal(20.83m,r.ConditionMatches.Single(c=>c.ConditionCode=="STRESS_ISOLATION").AssociationIndex);
        Assert.Equal(20.03m,r.ConditionMatches.Single(c=>c.ConditionCode=="CLOSED_ENVIRONMENT_AIR_QUALITY").AssociationIndex);
        Assert.Equal("ISOLATION",r.HazardScores.First().Hazard);
    }
    [Fact] public void SmsPhaseGate_UsesFiveDays_AndDoesNotGateOrthostaticAfterLaunch()
    {
        var codes=new[]{"NAUSEA_VOMITING","DIZZINESS","SWEATING","HEADACHE"};
        decimal Index(AssociationContext c)=>Evaluate(codes,c).ConditionMatches.Single(c=>c.ConditionCode=="SPACE_MOTION_SICKNESS").AssociationIndex;
        Assert.Equal(79.31m,Index(new(){MissionDay=2}));Assert.Equal(39.66m,Index(new(){MissionDay=40}));
        Assert.Equal(79.31m,Index(new(){MissionDay=40,HoursSinceGravityTransition=110}));
        var landed=Evaluate(["FAINTING","DIZZINESS"],new(){GravityTransitionKind="landing",HoursSinceGravityTransition=4});
        Assert.Equal(3,landed.Level);Assert.Equal(85m,landed.ConditionMatches.First().AssociationIndex);
        Assert.Equal(50m,Evaluate(["FAINTING","DIZZINESS"],new(){GravityTransitionKind="launch",HoursSinceGravityTransition=4}).ConditionMatches.Single(c=>c.ConditionCode=="ORTHOSTATIC_INTOLERANCE").AssociationIndex);
    }
    [Fact] public void NonspecificSymptoms_StayWeak_AndVisionHasCap()
    {
        Assert.All(Evaluate(["FATIGUE"]).ConditionMatches,c=>Assert.True(c.AssociationIndex<5));
        Assert.All(Evaluate(["HEADACHE"]).ConditionMatches,c=>Assert.True(c.AssociationIndex<10));
        Assert.Equal(42.86m,Evaluate(["BLURRED_VISION"]).ConditionMatches.Single().AssociationIndex);
        Assert.Equal(60m,Evaluate(["BLURRED_VISION","NEAR_VISION_CHANGE","HEADACHE"]).ConditionMatches.Single(c=>c.ConditionCode=="SPACEFLIGHT_VISION_CHANGES").AssociationIndex);
    }
    [Fact] public void RadiationRequiresRecordedEvent_AndNeverProducesNumericRadiationScore()
    {
        var codes=new[]{"NAUSEA_VOMITING","FATIGUE"};
        Assert.Equal("withheld",Evaluate(codes).Radiation.Status);
        var r=Evaluate(codes,new(){MissionDay=2,RadiationEventActive=true,CommsDelaySeconds=720});
        Assert.Equal("rule_triggered",r.Radiation.Status);Assert.Equal(2,r.Level);Assert.Equal("DELAYED",r.GuidanceMode);
        Assert.Equal(41.38m,r.ConditionMatches.Single(c=>c.ConditionCode=="SPACE_MOTION_SICKNESS").AssociationIndex);
        Assert.DoesNotContain(r.HazardScores,h=>h.Hazard=="RADIATION");
        Assert.Equal(1,Evaluate(["LIGHT_FLASHES"],new(){RadiationEventActive=true}).Level);
    }
    [Fact] public void KnowledgeOnlyAndUrgentSymptoms_AreNotScoredConditions()
    {
        var r=Evaluate(["NECK_FACE_SWELLING"]);Assert.Equal(2,r.Level);Assert.Empty(r.ConditionMatches);Assert.Single(r.KnowledgeOnly);
        Assert.Equal(2,Evaluate(["PALPITATIONS"]).Level);
        Assert.Equal(2,Evaluate(["FLANK_PAIN_HEMATURIA"]).KnowledgeOnly.Count);
        foreach(var code in db.Symptoms.Where(s=>s.IsRedFlag).Select(s=>s.Code).ToArray())Assert.Equal(3,Evaluate([code]).Level);
    }
    [Fact] public void DuplicateSymptomsDoNotIncreaseIndex_AndHazardUsesMaximum()
    {
        Assert.Equal(Evaluate(["HEADACHE"]).ConditionMatches.First().AssociationIndex,Evaluate(["HEADACHE","HEADACHE"]).ConditionMatches.First().AssociationIndex);
        var r=Evaluate(db.Symptoms.Select(s=>s.Code).ToArray());
        Assert.All(r.HazardScores,h=>Assert.Equal(r.ConditionMatches.Where(c=>c.HazardCode==h.Hazard).Max(c=>c.AssociationIndex),h.Index));
        Assert.All(r.ConditionMatches,c=>Assert.True(c.AssociationIndex<100));
        Assert.All(r.ConditionMatches,c=>Assert.NotNull(c.Breakdown));
    }
    [Fact] public void SerializedContract_ExposesPublicAssociationsButNoNumericKnowledgeCards()
    {
        var r=Evaluate(["NECK_FACE_SWELLING","HEADACHE"]);
        using var json=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(r));
        Assert.NotEmpty(json.RootElement.GetProperty("ConditionMatches").EnumerateArray());
        Assert.False(json.RootElement.GetProperty("KnowledgeOnly")[0].TryGetProperty("AssociationIndex",out _));
    }
    [Fact] public async Task Context_UsesRecordedEventsAndConsecutiveHistoryForTheTargetAstronaut()
    {
        var now=new DateTime(2026,11,13,12,0,0,DateTimeKind.Utc);
        db.Missions.Add(new(){Id=777,Code="V2-777",Name="Context",StartDate=now.AddDays(-1)});
        db.Astronauts.Add(new(){Id=777,MissionId=777,AstronautCode="V2-777",FullName="Context Test"});
        for(var day=0;day<3;day++)db.SleepRecords.Add(new(){AstronautId=777,EndTime=now.Date.AddDays(-day).AddHours(7),DurationHours=6.5,RecordedAt=now.AddDays(-day)});
        for(var day=0;day<2;day++)db.MoodEntries.Add(new(){AstronautId=777,MoodScaleMaximum=5,MoodScore=2,RecordedAt=now.AddDays(-day)});
        await db.SaveChangesAsync();
        var values=new Dictionary<string,string?> {
            ["SymptomAssessment:SleepBaselines:777"]="8",
            ["SymptomAssessment:Missions:777:CommsDelaySeconds"]="720",
            ["SymptomAssessment:Missions:777:RadiationEventAtUtc"]=now.AddHours(-6).ToString("O"),
            ["SymptomAssessment:Missions:777:Co2EventAtUtc"]=now.AddHours(-25).ToString("O")};
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var context=await AssociationContextResolver.ResolveAsync(db,777,config,now,default);
        Assert.Equal(2,context.MissionDay);Assert.True(context.RadiationEventActive);Assert.False(context.Co2EventActive);
        Assert.Equal(720,context.CommsDelaySeconds);Assert.True(context.SleepBelowBaselineThreeNights);Assert.True(context.MoodLowTwoDays);
        values["SymptomAssessment:Missions:777:RadiationEventAtUtc"]=now.AddHours(-49).ToString("O");
        var expired=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(values).Build();
        Assert.False((await AssociationContextResolver.ResolveAsync(db,777,expired,now,default)).RadiationEventActive);
    }
}
