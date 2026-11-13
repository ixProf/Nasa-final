using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Tests;

public class WellnessRuleTests
{
    [Fact]
    public void SleepPatterns_RequireActualConsecutiveNightsAndRespectBoundaries()
    {
        Assert.Null(WellnessRuleService.EvaluateSleep([4]));
        Assert.Null(WellnessRuleService.EvaluateSleep([7,7,7]));
        Assert.Null(WellnessRuleService.EvaluateSleep([5,5]));
        Assert.Equal(AlertSeverity.Emergency,WellnessRuleService.EvaluateSleep([4.9,4.9])!.Severity);
        Assert.Equal(AlertSeverity.MedicalAttention,WellnessRuleService.EvaluateSleep([6,6,6])!.Severity);
        Assert.Null(WellnessRuleService.EvaluateSleep([6.5,6.5,6.5]));
    }
    [Fact]
    public void MoodPatterns_DistinguishSingleLowScoreAndSustainedLowMood()
    {
        Assert.Null(WellnessRuleService.EvaluateMood([4,4,4]));
        Assert.Null(WellnessRuleService.EvaluateMood([2]));
        Assert.Equal(AlertSeverity.MedicalAttention,WellnessRuleService.EvaluateMood([3])!.Severity);
        Assert.Equal(AlertSeverity.MedicalAttention,WellnessRuleService.EvaluateMood([2,2])!.Severity);
        Assert.Equal(AlertSeverity.Emergency,WellnessRuleService.EvaluateMood([1])!.Severity);
        Assert.Equal(AlertSeverity.Emergency,WellnessRuleService.EvaluateMood([2,2,2,2,2])!.Severity);
    }
    [Fact]
    public async Task RepeatedPattern_DeduplicatesAlertsAndRecommendations_AndEscalates()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Database.EnsureCreated();
        var astronaut=new Astronaut{Id=321,AstronautCode="RULE-321",FullName="Rule Test"};db.Astronauts.Add(astronaut);await db.SaveChangesAsync();
        var service=new WellnessRuleService(db);
        var entry=new MoodEntry{AstronautId=321,Astronaut=astronaut,MoodScaleMaximum=5,MoodScore=3,RecordedAt=DateTime.UtcNow};
        await service.ApplyMoodAsync(entry,default);await db.SaveChangesAsync();
        await service.ApplyMoodAsync(entry,default);await db.SaveChangesAsync();
        entry.MoodScore=1;await service.ApplyMoodAsync(entry,default);await db.SaveChangesAsync();
        Assert.Equal(AlertSeverity.Emergency,Assert.Single(db.HealthAlerts.Where(a=>a.AstronautId==321)).Severity);
        var advice=Assert.Single(db.Recommendations.Where(r=>r.AstronautId==321));Assert.Equal("Automatic rule: MOOD",advice.PrescribedBy);Assert.False(string.IsNullOrEmpty(advice.ReasonAr));
    }
    [Fact]
    public async Task HistoricalTenPointScores_AreNotSilentlyInterpretedAsFivePointMood()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var result=await new WellnessRuleService(db).ApplyMoodAsync(new MoodEntry{MoodScaleMaximum=10,MoodScore=1},default);
        Assert.Null(result);
    }
}
