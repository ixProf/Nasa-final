using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstronautHealthMonitoring.Tests;

public class AssignmentReminderServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> options;
    private readonly AppDbContext db;
    private readonly ReminderClock clock = new();
    private readonly CaptureNotifier notifier = new();
    private readonly AssignmentReminderService service;
    public AssignmentReminderServiceTests()
    {
        options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        db = new AppDbContext(options); db.Database.EnsureCreated();
        var crew = new Astronaut { Id=1, AstronautCode="AST-1", FirstName="Test", LastName="Crew", FullName="Test Crew", IsActive=true };
        var task = new Recommendation { Id=1, Astronaut=crew, AstronautId=1, Title="Exercise", TitleAr="التمارين", Content="Activity", ContentAr="النشاط", IsDailyHabit=true };
        db.Recommendations.Add(task);
        db.AssignmentReminderSettings.Add(new AssignmentReminderSetting { Recommendation=task, Enabled=true, IntervalMinutes=5, SoundEnabled=true, NextDueAt=clock.Now.UtcDateTime.AddMinutes(5) });
        db.SaveChanges(); service=new(db,clock,notifier);
    }
    public void Dispose() => db.Dispose();

    [Fact]
    public async Task DueSchedule_SendsOnceAndAdvancesFromNow()
    {
        await service.DispatchDueAsync();Assert.Empty(notifier.Deliveries);
        clock.Now=clock.Now.AddMinutes(5);await service.DispatchDueAsync();await service.DispatchDueAsync();
        var delivery=Assert.Single(notifier.Deliveries);
        Assert.Equal(1,delivery.AstronautId);Assert.False(delivery.IsManual);Assert.True(delivery.SoundEnabled);
        Assert.Equal(clock.Now.UtcDateTime.AddMinutes(5),(await db.AssignmentReminderSettings.SingleAsync()).NextDueAt);
    }

    [Fact]
    public async Task ManualSend_DeliversImmediatelyAndCollapsesAccidentalDoubleClick()
    {
        await service.UpdateAsync(1,1,new(){Enabled=false,IntervalMinutes=60,SoundEnabled=false});
        var first=await service.SendAsync(1,true,"Monitor");var second=await service.SendAsync(1,true,"Monitor");
        Assert.NotNull(first);Assert.Equal(first.Token,second!.Token);Assert.False(first.SoundEnabled);
        Assert.Single(notifier.Deliveries);
        clock.Now=clock.Now.AddSeconds(6);var third=await service.SendAsync(1,true,"Monitor");
        Assert.NotEqual(first.Token,third!.Token);Assert.Equal(2,notifier.Deliveries.Count);
        Assert.Single(await db.AssignmentReminderDeliveries.ToListAsync());
    }

    [Fact]
    public async Task AstronautPreference_OnlyOwnTaskAndDisableStopsAutomaticDelivery()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.UpdateAsync(1,2,new(){Enabled=false}));
        await service.UpdateAsync(1,1,new(){Enabled=false,IntervalMinutes=5});
        clock.Now=clock.Now.AddHours(1);await service.DispatchDueAsync();Assert.Empty(notifier.Deliveries);
    }

    [Fact]
    public async Task OfflineCatchUp_ReturnsOnlyLatestReminderPerTask()
    {
        clock.Now=clock.Now.AddMinutes(5);await service.DispatchDueAsync();
        clock.Now=clock.Now.AddHours(3);await service.DispatchDueAsync();
        var inbox=await new AssignmentReminderService(db,clock,notifier).GetAsync(1);
        Assert.Single(inbox.Pending);Assert.Equal(notifier.Deliveries.Last().Token,inbox.Pending[0].Token);
        Assert.Empty((await service.GetAsync(2)).Pending);
    }

    [Fact]
    public async Task AcknowledgingOldPopup_DoesNotDismissANewerReminder()
    {
        var first=(await service.SendAsync(1,true))!;clock.Now=clock.Now.AddSeconds(6);
        var next=(await service.SendAsync(1,true))!;
        await service.AcknowledgeAsync(1,1,first.Token);
        Assert.Equal(next.Token,Assert.Single((await service.GetAsync(1)).Pending).Token);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.AcknowledgeAsync(1,2,next.Token));
        await service.AcknowledgeAsync(1,1,next.Token);Assert.Empty((await service.GetAsync(1)).Pending);
    }

    [Fact]
    public async Task CompletedDailyHabit_PausesUntilNextUtcDay()
    {
        db.HabitCompletions.Add(new(){RecommendationId=1,AstronautId=1,CompletedOn=DateOnly.FromDateTime(clock.Now.UtcDateTime),CompletedAt=clock.Now.UtcDateTime});
        await db.SaveChangesAsync();clock.Now=clock.Now.AddHours(1);await service.DispatchDueAsync();Assert.Empty(notifier.Deliveries);
        Assert.True((await service.GetAsync(1)).Assignments[0].CompletedToday);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SendAsync(1,true));
        clock.Now=clock.Now.AddDays(1);await service.DispatchDueAsync();Assert.Single(notifier.Deliveries);
    }

    [Fact]
    public async Task CompletedOneOffAssignment_NeverReminds()
    {
        var r=await db.Recommendations.SingleAsync();r.IsDailyHabit=false;r.Status=RecommendationStatus.Completed;
        await db.SaveChangesAsync();clock.Now=clock.Now.AddHours(1);await service.DispatchDueAsync();
        Assert.Empty(notifier.Deliveries);Assert.Empty((await service.GetAsync(1)).Assignments);
    }

    [Fact]
    public async Task Creation_PersistsRepeatAndSendsInitialReminder()
    {
        var config=new TypeAdapterConfig();new RecommendationMappings().Register(config);
        var recommendations=new RecommendationService(db,new Mapper(config),NullLogger<RecommendationService>.Instance,notifier,clock);
        var task=await recommendations.CreateRecommendationAsync(1,new(){AstronautId=1,Title="New task",Content="Do task",Reason="Plan",ReminderEnabled=true,ReminderIntervalMinutes=120,ReminderSoundEnabled=true});
        var setting=await db.AssignmentReminderSettings.SingleAsync(s=>s.RecommendationId==task.Id);
        Assert.Equal(120,setting.IntervalMinutes);Assert.Equal(clock.Now.UtcDateTime.AddHours(2),setting.NextDueAt);
        Assert.Equal(task.Id,Assert.Single(notifier.Deliveries).RecommendationId);
    }

    [Fact]
    public async Task DismissedPopup_DoesNotCancelTheRepeatSchedule()
    {
        var first=(await service.SendAsync(1,true))!;
        await service.AcknowledgeAsync(1,1,first.Token);
        clock.Now=clock.Now.AddMinutes(5);await service.DispatchDueAsync();
        var next=Assert.Single((await service.GetAsync(1)).Pending);
        Assert.NotEqual(first.Token,next.Token);
        Assert.True((await db.AssignmentReminderSettings.SingleAsync()).Enabled);
    }

    [Fact]
    public async Task ConcurrentAcknowledgement_CannotMarkNewDeliveryAsRead()
    {
        var first=(await service.SendAsync(1,true))!;
        using var interleavedDb=new InterleavedDb(options);
        var acknowledger=new AssignmentReminderService(interleavedDb,clock,notifier);
        AssignmentReminderDeliveryDto? newer=null;
        interleavedDb.BeforeSave=async()=>{clock.Now=clock.Now.AddSeconds(6);newer=await service.SendAsync(1,true);};
        await acknowledger.AcknowledgeAsync(1,1,first.Token);
        Assert.NotNull(newer);
        Assert.Equal(newer.Token,Assert.Single((await service.GetAsync(1)).Pending).Token);
    }

    [Fact]
    public async Task CreationWithoutReminders_DoesNotSendAnInitialPopup()
    {
        var config=new TypeAdapterConfig();new RecommendationMappings().Register(config);
        var recommendations=new RecommendationService(db,new Mapper(config),NullLogger<RecommendationService>.Instance,notifier,clock);
        var task=await recommendations.CreateRecommendationAsync(1,new(){AstronautId=1,Title="Quiet task",Content="Task",Reason="Plan",ReminderEnabled=false});
        Assert.Empty(notifier.Deliveries);
        Assert.False((await db.AssignmentReminderSettings.SingleAsync(s=>s.RecommendationId==task.Id)).Enabled);
    }

    [Fact]
    public async Task RevisionGuard_RejectsConcurrentPreferenceChanges()
    {
        using var otherDb=new AppDbContext(options);
        var other=new AssignmentReminderService(otherDb,clock,notifier);
        await other.UpdateAsync(1,1,new(){Enabled=true,IntervalMinutes=15});
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>service.UpdateAsync(1,1,new(){Enabled=true,IntervalMinutes=30}));
    }

    [Fact]
    public async Task InvalidIntervals_AreRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(()=>service.UpdateAsync(1,1,new(){Enabled=true,IntervalMinutes=0}));
        await Assert.ThrowsAsync<ArgumentException>(()=>service.UpdateAsync(1,1,new(){Enabled=true,IntervalMinutes=10081}));
    }

    private class ReminderClock : TimeProvider
    {
        public DateTimeOffset Now {get;set;}=new(2026,11,13,12,0,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow()=>Now;
    }
    private class InterleavedDb(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public Func<Task>? BeforeSave {get;set;}
        public override async Task<int> SaveChangesAsync(CancellationToken ct=default)
        {
            var action=BeforeSave;BeforeSave=null;
            if(action!=null)await action();
            return await base.SaveChangesAsync(ct);
        }
    }
    private class CaptureNotifier : IAssignmentReminderNotifier
    {
        public List<AssignmentReminderDeliveryDto> Deliveries {get;}=[];
        public Task NotifyAsync(AssignmentReminderDeliveryDto delivery,CancellationToken ct=default){Deliveries.Add(delivery);return Task.CompletedTask;}
    }
}
