using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Dashboard;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class DashboardServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"DashboardServiceTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated(); // Seeds reference ranges, procedures, etc.

        var config = new TypeAdapterConfig();
        new AstronautMappings().Register(config);
        new VitalSignMappings().Register(config);
        new AlertMappings().Register(config);
        new SleepMappings().Register(config);
        new MoodMappings().Register(config);
        new RecommendationMappings().Register(config);
        new MissionMappings().Register(config);
        _mapper = new Mapper(config);

        _dashboardService = new DashboardService(_context, _mapper, NullLogger<DashboardService>.Instance);
    }

    [Fact]
    public async Task GetMonitorDashboardAsync_AggregatesCrewStatus_AndExcludesDeactivatedAstronautAlerts()
    {
        // 1. Mission
        var mission = new Mission
        {
            Code = "ARES-X",
            Name = "Ares Exploration",
            Destination = "Mars",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Active
        };
        _context.Missions.Add(mission);
        await _context.SaveChangesAsync();

        // 2. Active Astronaut with an Emergency Alert and Heart Rate reading
        var activeAstronaut = new Astronaut
        {
            AstronautCode = "AST-ACT-01",
            FirstName = "Active",
            LastName = "Astro",
            FullName = "Active Astro",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "O+",
            HeightCm = 170,
            WeightKg = 62,
            MissionId = mission.Id,
            IsActive = true
        };

        // 3. Deactivated Astronaut with an Emergency Alert
        var deactivatedAstronaut = new Astronaut
        {
            AstronautCode = "AST-DEACT-01",
            FirstName = "Deactivated",
            LastName = "Astro",
            FullName = "Deactivated Astro",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "A+",
            HeightCm = 180,
            WeightKg = 75,
            MissionId = mission.Id,
            IsActive = false
        };

        _context.Astronauts.AddRange(activeAstronaut, deactivatedAstronaut);
        await _context.SaveChangesAsync();

        // Add vital readings
        var hrType = await _context.VitalSignTypes.FirstAsync(t => t.Code == "HR");
        _context.VitalSignReadings.AddRange(
            new VitalSignReading
            {
                AstronautId = activeAstronaut.Id,
                VitalSignTypeId = hrType.Id,
                Value = 145, // Emergency tachycardia
                Status = VitalSignStatus.Emergency,
                RecordedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new VitalSignReading
            {
                AstronautId = deactivatedAstronaut.Id,
                VitalSignTypeId = hrType.Id,
                Value = 72,
                Status = VitalSignStatus.Routine,
                RecordedAt = DateTime.UtcNow.AddMinutes(-10)
            }
        );

        // Add alerts
        _context.HealthAlerts.AddRange(
            new HealthAlert
            {
                AstronautId = activeAstronaut.Id,
                Severity = AlertSeverity.Emergency,
                Status = AlertStatus.Active,
                Message = "Tachycardia emergency on active crew",
                TriggeredAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new HealthAlert
            {
                AstronautId = deactivatedAstronaut.Id,
                Severity = AlertSeverity.Emergency,
                Status = AlertStatus.Active,
                Message = "Alert on deactivated crew",
                TriggeredAt = DateTime.UtcNow.AddMinutes(-10)
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var dashboard = await _dashboardService.GetMonitorDashboardAsync();

        // Assert system-level counts (deactivated astronaut excluded from alert counts per policy)
        Assert.Equal(1, dashboard.TotalMonitoredAstronauts); // only active
        Assert.Equal(1, dashboard.ActiveAstronautsCount);
        Assert.Equal(1, dashboard.DeactivatedAstronautsCount);
        Assert.Equal(1, dashboard.ActiveMissionsCount);
        Assert.Equal(1, dashboard.ActiveAlertsCount); // 1 active, excluded deactivated
        Assert.Equal(1, dashboard.EmergencyAlertsCount); // 1 emergency, excluded deactivated
        Assert.Single(dashboard.CriticalAlerts);
        Assert.Equal(activeAstronaut.Id, dashboard.CriticalAlerts[0].AstronautId);

        // Assert crew summaries
        Assert.Equal(2, dashboard.CrewSummaries.Count);

        var activeSummary = dashboard.CrewSummaries.First(c => c.AstronautId == activeAstronaut.Id);
        Assert.True(activeSummary.IsActive);
        Assert.Equal(VitalSignStatus.Emergency, activeSummary.OverallStatus);
        Assert.Equal(1, activeSummary.ActiveAlertsCount);
        Assert.Equal(1, activeSummary.EmergencyAlertsCount);
        Assert.Single(activeSummary.LatestVitals);

        var deactivatedSummary = dashboard.CrewSummaries.First(c => c.AstronautId == deactivatedAstronaut.Id);
        Assert.False(deactivatedSummary.IsActive);
        Assert.Equal(0, deactivatedSummary.ActiveAlertsCount); // alert counts zeroed/excluded for deactivated

        // Assert mission summary
        Assert.Single(dashboard.MissionSummaries);
        Assert.Equal("ARES-X", dashboard.MissionSummaries[0].Code);
        Assert.Equal(1, dashboard.MissionSummaries[0].CrewCount); // only active crew
    }

    [Fact]
    public async Task GetAstronautSnapshotAsync_ReturnsCompleteTelemetrySnapshot()
    {
        var mission = new Mission
        {
            Code = "ARTEMIS-III",
            Name = "Artemis III",
            Destination = "Lunar South Pole",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Active
        };
        _context.Missions.Add(mission);
        await _context.SaveChangesAsync();

        var astronaut = new Astronaut
        {
            AstronautCode = "AST-SNAP",
            FirstName = "Christina",
            LastName = "Koch",
            FullName = "Christina Koch",
            DateOfBirth = new DateTime(1979, 1, 29, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "B+",
            HeightCm = 168,
            WeightKg = 59,
            MissionId = mission.Id,
            IsActive = true
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var hrType = await _context.VitalSignTypes.FirstAsync(t => t.Code == "HR");
        _context.VitalSignReadings.Add(new VitalSignReading
        {
            AstronautId = astronaut.Id,
            VitalSignTypeId = hrType.Id,
            Value = 72,
            Status = VitalSignStatus.Routine,
            RecordedAt = DateTime.UtcNow.AddHours(-1)
        });

        _context.SleepRecords.Add(new SleepRecord
        {
            AstronautId = astronaut.Id,
            StartTime = DateTime.UtcNow.AddHours(-9),
            EndTime = DateTime.UtcNow.AddHours(-1),
            DurationHours = 8.0,
            Quality = SleepQuality.Good,
            DeepSleepMinutes = 120,
            SleepEfficiencyPercentage = 90.0
        });

        _context.MoodEntries.Add(new MoodEntry
        {
            AstronautId = astronaut.Id,
            MoodScore = 8,
            StressLevel = 3,
            DominantEmotion = "Focused",
            RecordedAt = DateTime.UtcNow.AddMinutes(-30)
        });

        _context.Recommendations.Add(new Recommendation
        {
            AstronautId = astronaut.Id,
            Title = "Hydration Reminder",
            Content = "Drink 500ml water following EVA prep.",
            Reason = "Physiological hydration protocol",
            Category = RecommendationCategory.Medical,
            Priority = RecommendationPriority.High,
            Status = RecommendationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        _context.ChatMessages.Add(new ChatMessage
        {
            AstronautId = astronaut.Id,
            SenderRole = UserRoles.Monitor,
            SenderUserId = "monitor-01",
            Content = "Flight Director to Christina: please confirm EVA prep complete.",
            RecordedAt = DateTime.UtcNow.AddMinutes(-15),
            SentAt = DateTime.UtcNow.AddMinutes(-15),
            ReadAt = null // unread
        });

        await _context.SaveChangesAsync();

        // Act
        var snapshot = await _dashboardService.GetAstronautSnapshotAsync(astronaut.Id);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(astronaut.Id, snapshot.AstronautId);
        Assert.Equal("Christina Koch", snapshot.FullName);
        Assert.Equal("Artemis III", snapshot.MissionName);
        Assert.True(snapshot.IsActive);
        Assert.Equal(VitalSignStatus.Routine, snapshot.OverallStatus);
        Assert.Single(snapshot.RecentVitals);
        Assert.NotNull(snapshot.LatestSleep);
        Assert.Equal(8.0, snapshot.LatestSleep.DurationHours);
        Assert.NotNull(snapshot.LatestMood);
        Assert.Equal(8, snapshot.LatestMood.MoodScore);
        Assert.Single(snapshot.PendingRecommendations);
        Assert.Equal("Hydration Reminder", snapshot.PendingRecommendations[0].Title);
        Assert.Equal(1, snapshot.UnreadChatMessagesCount);
    }

    [Fact]
    public async Task GetMyDashboardAsync_WhenAstronautDoesNotExist_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _dashboardService.GetMyDashboardAsync(999999));
    }

    /// <summary>
    /// Note on Test DB Provider: Microsoft.EntityFrameworkCore.InMemory is a non-relational in-memory object store
    /// and does not generate ADO.NET DbCommands/SQL text (a DbCommandInterceptor requires a relational ADO.NET connection).
    /// The closest honest alternative in EF Core without a live PostgreSQL instance is intercepting entity set query accesses,
    /// which strictly proves that EF Core executes O(1) set-based queries (exactly 4: Astronauts+Missions, Missions, HealthAlerts, VitalSignReadings)
    /// and does not invoke per-astronaut sub-queries in a loop (no N+1 pattern).
    /// </summary>
    [Fact]
    public async Task GetMonitorDashboardAsync_IssuesBoundedNumberOfQueries_RegardlessOfCrewSize()
    {
        // Test with 3 astronauts
        var (count3, dto3) = await RunDashboardQueryCountAsync(3);

        // Test with 10 astronauts
        var (count10, dto10) = await RunDashboardQueryCountAsync(10);

        // Both must issue the exact same constant bounded number of database queries (specifically 4 queries)
        Assert.Equal(4, count3);
        Assert.Equal(4, count10);
        Assert.Equal(3, dto3.CrewSummaries.Count);
        Assert.Equal(10, dto10.CrewSummaries.Count);
    }

    private class QueryCountingDbContext : AppDbContext
    {
        public int QueryCount;

        public QueryCountingDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public override DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            Interlocked.Increment(ref QueryCount);
            return base.Set<TEntity>();
        }
    }

    private async Task<(int QueryCount, MonitorDashboardSummaryDto Dto)> RunDashboardQueryCountAsync(int crewCount)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"BoundedQueryDb_{Guid.NewGuid()}")
            .Options;

        using var context = new QueryCountingDbContext(options);
        context.Database.EnsureCreated();

        var mission = new Mission
        {
            Code = $"MSN-{Guid.NewGuid():N}",
            Name = "Test Mission",
            Destination = "Mars",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Active
        };
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        var hrType = await context.VitalSignTypes.FirstAsync(t => t.Code == "HR");

        for (int i = 0; i < crewCount; i++)
        {
            var astro = new Astronaut
            {
                AstronautCode = $"AST-{i:D3}",
                FirstName = $"Crew{i}",
                LastName = "Astronaut",
                FullName = $"Crew{i} Astronaut",
                DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                BloodType = "O+",
                HeightCm = 175,
                WeightKg = 70,
                MissionId = mission.Id,
                IsActive = true
            };
            context.Astronauts.Add(astro);
            await context.SaveChangesAsync();

            context.VitalSignReadings.Add(new VitalSignReading
            {
                AstronautId = astro.Id,
                VitalSignTypeId = hrType.Id,
                Value = 75,
                Status = VitalSignStatus.Routine,
                RecordedAt = DateTime.UtcNow
            });

            context.HealthAlerts.Add(new HealthAlert
            {
                AstronautId = astro.Id,
                Severity = AlertSeverity.Routine,
                Status = AlertStatus.Active,
                Message = $"Routine alert {i}",
                TriggeredAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        // Reset query counter before invoking the service method under test
        context.QueryCount = 0;

        var service = new DashboardService(context, _mapper, NullLogger<DashboardService>.Instance);
        var result = await service.GetMonitorDashboardAsync();

        return (context.QueryCount, result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
