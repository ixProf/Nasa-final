using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace AstronautHealthMonitoring.Tests;

public class HabitServiceTests : IDisposable
{
    private readonly AppDbContext db;
    private readonly DemoClock clock = new();
    private readonly HabitService service;
    public HabitServiceTests()
    {
        db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Database.EnsureCreated();
        var astronaut = new Astronaut { Id = 1, AstronautCode = "TEST-1", FirstName = "Test", LastName = "Crew", FullName = "Test Crew", IsActive = true };
        db.Astronauts.Add(astronaut);
        db.Recommendations.Add(new Recommendation { Id = 1, AstronautId = 1, Astronaut = astronaut, Title = "Exercise", TitleAr = "التمارين", Content = "Complete your assigned exercise", ContentAr = "أكمل التمرين المحدد", Reason = "Daily activity", ReasonAr = "النشاط اليومي", IsDailyHabit = true, HabitPoints = 15, GeneratedAt = clock.GetUtcNow().UtcDateTime });
        db.SaveChanges();
        service = new HabitService(db, clock);
    }
    public void Dispose() => db.Dispose();

    [Fact]
    public async Task RepeatedCompletion_AwardsOnceAndSurvivesReload()
    {
        await service.CompleteAsync(1, 1);
        await service.CompleteAsync(1, 1);
        var reloaded = await new HabitService(db, clock).GetAsync(1);
        Assert.Single(await db.HabitCompletions.ToListAsync());
        Assert.Equal(15, reloaded.TotalPoints);
        Assert.Equal(1, reloaded.CurrentStreak);
        Assert.True(Assert.Single(reloaded.Assignments).CompletedToday);
        Assert.Equal("التمارين", reloaded.Assignments[0].TitleAr);
    }

    [Fact]
    public async Task RecommendationCreation_PreservesHabitAndBothLanguages()
    {
        var config = new Mapster.TypeAdapterConfig();
        new AstronautHealthMonitoring.Mapping.RecommendationMappings().Register(config);
        var recommendations = new RecommendationService(db, new MapsterMapper.Mapper(config), Microsoft.Extensions.Logging.Abstractions.NullLogger<RecommendationService>.Instance);
        var result = await recommendations.CreateRecommendationAsync(1, new AstronautHealthMonitoring.DTOs.Recommendations.CreateRecommendationRequest
        {
            AstronautId=1, Title="Hydration", TitleAr="الترطيب", Content="Follow your hydration plan", ContentAr="اتبع خطة الترطيب", Reason="Daily plan", ReasonAr="الخطة اليومية",
            IsDailyHabit=true, HabitPoints=12, ScheduledTime="08:30", PrescribedBy="Flight surgeon"
        });
        Assert.True(result.IsDailyHabit);
        Assert.Equal("DailyRecurring",result.ScheduleType);
        Assert.Equal(12,result.HabitPoints);
        Assert.Equal("08:30",result.ScheduledTime);
        Assert.Equal("الترطيب",result.TitleAr);
        Assert.Equal("اتبع خطة الترطيب",result.ContentAr);
    }

    [Fact]
    public void DailyAssignment_RequiresArabicTextAndBoundedPoints()
    {
        var validator = new AstronautHealthMonitoring.Validators.Recommendations.CreateRecommendationRequestValidator();
        var result = validator.Validate(new AstronautHealthMonitoring.DTOs.Recommendations.CreateRecommendationRequest
        { AstronautId=1, Title="Task", Content="Task", Reason="Plan", IsDailyHabit=true, HabitPoints=101 });
        Assert.Contains(result.Errors,e=>e.PropertyName=="TitleAr");
        Assert.Contains(result.Errors,e=>e.PropertyName=="ContentAr");
        Assert.Contains(result.Errors,e=>e.PropertyName=="ReasonAr");
        Assert.Contains(result.Errors,e=>e.PropertyName=="HabitPoints");
    }

    [Fact]
    public async Task AnotherAstronaut_CannotCompleteAssignment()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CompleteAsync(1, 2));
        Assert.Empty(await db.HabitCompletions.ToListAsync());
    }

    [Fact]
    public async Task DismissedHabit_CannotAwardPoints()
    {
        (await db.Recommendations.SingleAsync()).Status = RecommendationStatus.Dismissed;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(1, 1));
    }

    [Fact]
    public async Task UtcMidnight_ResetsDailyTaskAndKeepsHistoricalPoints()
    {
        await service.CompleteAsync(1, 1);
        clock.Now = clock.Now.AddDays(1);
        Assert.False((await service.GetAsync(1)).Assignments[0].CompletedToday);
        (await db.Recommendations.SingleAsync()).HabitPoints = 20;
        await db.SaveChangesAsync();
        var result = await service.CompleteAsync(1, 1);
        Assert.Equal(35, result.TotalPoints);
        Assert.Equal(2, result.CurrentStreak);
        Assert.Equal(2, result.TotalCompletions);
    }

    [Fact]
    public void Heatmap_ContainsZeroDaysAndStreakUsesConsecutiveDays()
    {
        var today = new DateOnly(2026, 11, 13);
        var rows = new[] { -5, -4, -3, -1 }.Select(offset => new HabitCompletion { CompletedOn = today.AddDays(offset), Points = 10 });
        var result = HabitService.Summarize(rows, today);
        Assert.Equal(84, result.Heatmap.Count);
        Assert.Equal(1, result.CurrentStreak);
        Assert.Equal(3, result.LongestStreak);
        Assert.Equal(0, result.Heatmap.Last().Count);
        Assert.Equal(40, result.TotalPoints);
    }

    [Fact]
    public void Badges_AndLevel_AreDerivedFromSavedCompletions()
    {
        var today = new DateOnly(2026, 11, 13);
        var result = HabitService.Summarize(Enumerable.Range(0, 7).Select(i => new HabitCompletion { CompletedOn = today.AddDays(-i), Points = 20 }), today);
        Assert.Equal(2, result.Level);
        Assert.Contains("week-streak", result.Badges);
        Assert.Contains("century", result.Badges);
        Assert.DoesNotContain("month-streak", result.Badges);
    }
    private class DemoClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
