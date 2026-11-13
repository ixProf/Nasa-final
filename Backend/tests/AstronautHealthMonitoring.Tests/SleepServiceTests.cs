using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class SleepServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TestCurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ISleepService _sleepService;

    public SleepServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"SleepServiceTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        if (!_context.Astronauts.Any(a => a.Id == 1))
        {
            _context.Astronauts.Add(new Astronaut
            {
                Id = 1,
                AstronautCode = "AST-001",
                FirstName = "Alex",
                LastName = "Vance",
                FullName = "Alex Vance",
                DateOfBirth = new DateTime(1990, 5, 12, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                BloodType = "O+",
                HeightCm = 182,
                WeightKg = 78
            });
        }

        if (!_context.Astronauts.Any(a => a.Id == 2))
        {
            _context.Astronauts.Add(new Astronaut
            {
                Id = 2,
                AstronautCode = "AST-002",
                FirstName = "Morgan",
                LastName = "Cruz",
                FullName = "Morgan Cruz",
                DateOfBirth = new DateTime(1988, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Female",
                BloodType = "A+",
                HeightCm = 170,
                WeightKg = 64
            });
        }
        _context.SaveChanges();

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Astronaut,
            AstronautId = 1,
            UserId = "user-astro-1"
        };

        var config = new TypeAdapterConfig();
        config.Scan(typeof(SleepMappings).Assembly);
        _mapper = new ServiceMapper(null!, config);

        _sleepService = new SleepService(_context, _currentUserService, _mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task RecordSleepAsync_WhenValid_SavesRecord_CalculatesDurationAndEfficiency()
    {
        var recordedAt = DateTime.UtcNow;
        var start = recordedAt.AddHours(-8);
        var end = recordedAt;

        var request = new CreateSleepRecordRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = recordedAt,
            StartTime = start,
            EndTime = end,
            Quality = SleepQuality.Good,
            DeepSleepMinutes = 120,
            RemSleepMinutes = 90,
            LightSleepMinutes = 240,
            AwakeMinutes = 30,
            SleepEfficiencyPercentage = 93.75,
            Notes = "Well rested after orbital maintenance."
        };

        var result = await _sleepService.RecordSleepAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal(1, result.AstronautId);
        Assert.Equal(8.0, result.DurationHours);
        Assert.Equal(93.75, result.SleepEfficiencyPercentage);
        Assert.Equal(SleepQuality.Good, result.Quality);

        var dbRecord = await _context.SleepRecords.FindAsync(result.Id);
        Assert.NotNull(dbRecord);
        Assert.Equal(8.0, dbRecord.DurationHours);
        Assert.Equal(93.75, dbRecord.SleepEfficiencyPercentage);
    }

    [Fact]
    public async Task RecordSleepAsync_Idempotency_DuplicateClientGeneratedId_ReturnsExistingRecord()
    {
        var clientGenId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var request = new CreateSleepRecordRequest
        {
            ClientGeneratedId = clientGenId,
            RecordedAt = now,
            StartTime = now.AddHours(-7),
            EndTime = now,
            Quality = SleepQuality.Fair,
            DeepSleepMinutes = 80,
            RemSleepMinutes = 60,
            LightSleepMinutes = 240,
            AwakeMinutes = 40,
            SleepEfficiencyPercentage = 90.0
        };

        var first = await _sleepService.RecordSleepAsync(1, request);
        var second = await _sleepService.RecordSleepAsync(1, request);

        Assert.Equal(first.Id, second.Id);
        var count = await _context.SleepRecords.CountAsync(s => s.ClientGeneratedId == clientGenId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RecordSleepAsync_WhenAstronautAttemptsForAnotherAstronaut_ThrowsUnauthorizedAccessException()
    {
        var request = new CreateSleepRecordRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = DateTime.UtcNow,
            StartTime = DateTime.UtcNow.AddHours(-7),
            EndTime = DateTime.UtcNow
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sleepService.RecordSleepAsync(2, request));

        Assert.Contains("Astronauts can only record sleep records for themselves", ex.Message);
    }

    [Fact]
    public async Task RecordSleepAsync_WhenMonitorAttemptsToRecord_ThrowsUnauthorizedAccessException()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var request = new CreateSleepRecordRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = DateTime.UtcNow,
            StartTime = DateTime.UtcNow.AddHours(-7),
            EndTime = DateTime.UtcNow
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sleepService.RecordSleepAsync(1, request));

        Assert.Contains("Flight monitors cannot create sleep records", ex.Message);
    }

    [Fact]
    public async Task GetSleepRecordByIdAsync_AstronautCannotViewAnotherAstronautsRecord()
    {
        var record = new SleepRecord
        {
            ClientGeneratedId = Guid.NewGuid(),
            AstronautId = 2,
            RecordedAt = DateTime.UtcNow,
            StartTime = DateTime.UtcNow.AddHours(-8),
            EndTime = DateTime.UtcNow,
            DurationHours = 8.0,
            Quality = SleepQuality.Good,
            CreatedAt = DateTime.UtcNow
        };
        _context.SleepRecords.Add(record);
        await _context.SaveChangesAsync();

        // Current user is Astronaut 1
        var result = await _sleepService.GetSleepRecordByIdAsync(record.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSleepHistoryPagedAsync_RespectsDateRangeAndPagination()
    {
        var baseTime = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 10; i++)
        {
            _context.SleepRecords.Add(new SleepRecord
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                RecordedAt = baseTime.AddDays(i),
                StartTime = baseTime.AddDays(i).AddHours(-8),
                EndTime = baseTime.AddDays(i),
                DurationHours = 8.0,
                Quality = SleepQuality.Good,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Filter: Days 2 to 6 (5 records), page size 3, page 1
        var paged = await _sleepService.GetSleepHistoryPagedAsync(
            1,
            fromUtc: baseTime.AddDays(2),
            toUtc: baseTime.AddDays(6),
            pageNumber: 1,
            pageSize: 3);

        Assert.Equal(5, paged.TotalCount);
        Assert.Equal(3, paged.Items.Count);
        Assert.Equal(2, paged.TotalPages);
        Assert.True(paged.HasNextPage);
    }

    [Fact]
    public async Task GetLatestSleepRecordAsync_ReturnsLatestRecord()
    {
        var now = DateTime.UtcNow;
        _context.SleepRecords.AddRange(
            new SleepRecord
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                RecordedAt = now.AddDays(-2),
                StartTime = now.AddDays(-2).AddHours(-7),
                EndTime = now.AddDays(-2),
                DurationHours = 7.0,
                Quality = SleepQuality.Fair,
                CreatedAt = now.AddDays(-2)
            },
            new SleepRecord
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                RecordedAt = now.AddDays(-1),
                StartTime = now.AddDays(-1).AddHours(-8.5),
                EndTime = now.AddDays(-1),
                DurationHours = 8.5,
                Quality = SleepQuality.Excellent,
                CreatedAt = now.AddDays(-1)
            }
        );
        await _context.SaveChangesAsync();

        var latest = await _sleepService.GetLatestSleepRecordAsync(1);
        Assert.NotNull(latest);
        Assert.Equal(8.5, latest.DurationHours);
        Assert.Equal(SleepQuality.Excellent, latest.Quality);
    }

    [Fact]
    public async Task MonitorCanViewAnyAstronautsSleepRecord()
    {
        var record = new SleepRecord
        {
            ClientGeneratedId = Guid.NewGuid(),
            AstronautId = 2,
            RecordedAt = DateTime.UtcNow,
            StartTime = DateTime.UtcNow.AddHours(-8),
            EndTime = DateTime.UtcNow,
            DurationHours = 8.0,
            Quality = SleepQuality.Good,
            CreatedAt = DateTime.UtcNow
        };
        _context.SleepRecords.Add(record);
        await _context.SaveChangesAsync();

        // Switch to Monitor role
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var result = await _sleepService.GetSleepRecordByIdAsync(record.Id);
        Assert.NotNull(result);
        Assert.Equal(2, result.AstronautId);

        var history = await _sleepService.GetSleepHistoryPagedAsync(2, null, null, 1, 10);
        Assert.NotNull(history);
        Assert.NotEmpty(history.Items);
    }

    [Fact]
    public async Task RecordSleepAsync_WhenAstronautIsDeactivated_ThrowsInvalidOperationException()
    {
        var astro = await _context.Astronauts.FindAsync(1);
        Assert.NotNull(astro);
        astro.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new CreateSleepRecordRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddHours(-8),
            EndTime = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            LightSleepMinutes = 240,
            DeepSleepMinutes = 120,
            RemSleepMinutes = 90,
            AwakeMinutes = 30
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sleepService.RecordSleepAsync(1, request));

        Assert.Contains("Cannot record sleep for deactivated astronaut", ex.Message);
    }
}

