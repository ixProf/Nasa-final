using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class MoodServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TestCurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IMoodService _moodService;

    public MoodServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"MoodServiceTestDb_{Guid.NewGuid()}")
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
        config.Scan(typeof(MoodMappings).Assembly);
        _mapper = new ServiceMapper(null!, config);

        _moodService = new MoodService(_context, _currentUserService, _mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task RecordMoodAsync_WhenValid_SavesEntry_SetsServerFields()
    {
        var recordedAt = DateTime.UtcNow;
        var request = new CreateMoodEntryRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = recordedAt,
            MoodScore = 8,
            StressLevel = 3,
            DominantEmotion = "Calm",
            Notes = "Completed mission checklists smoothly."
        };

        var result = await _moodService.RecordMoodAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal(1, result.AstronautId);
        Assert.Equal(8, result.MoodScore);
        Assert.Equal(3, result.StressLevel);
        Assert.Equal("Calm", result.DominantEmotion);

        var dbEntry = await _context.MoodEntries.FindAsync(result.Id);
        Assert.NotNull(dbEntry);
        Assert.Equal(1, dbEntry.AstronautId);
        Assert.Equal(8, dbEntry.MoodScore);
    }

    [Fact]
    public async Task RecordMoodAsync_Idempotency_DuplicateClientGeneratedId_ReturnsExistingEntry()
    {
        var clientGenId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var request = new CreateMoodEntryRequest
        {
            ClientGeneratedId = clientGenId,
            RecordedAt = now,
            MoodScore = 7,
            StressLevel = 4,
            DominantEmotion = "Focused"
        };

        var first = await _moodService.RecordMoodAsync(1, request);
        var second = await _moodService.RecordMoodAsync(1, request);

        Assert.Equal(first.Id, second.Id);
        var count = await _context.MoodEntries.CountAsync(m => m.ClientGeneratedId == clientGenId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RecordMoodAsync_WhenAstronautAttemptsForAnotherAstronaut_ThrowsUnauthorizedAccessException()
    {
        var request = new CreateMoodEntryRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = DateTime.UtcNow,
            MoodScore = 7,
            StressLevel = 4,
            DominantEmotion = "Focused"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _moodService.RecordMoodAsync(2, request));

        Assert.Contains("Astronauts can only record mood entries for themselves", ex.Message);
    }

    [Fact]
    public async Task RecordMoodAsync_WhenMonitorAttemptsToRecord_ThrowsUnauthorizedAccessException()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var request = new CreateMoodEntryRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = DateTime.UtcNow,
            MoodScore = 7,
            StressLevel = 4,
            DominantEmotion = "Focused"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _moodService.RecordMoodAsync(1, request));

        Assert.Contains("Flight monitors cannot create mood entries", ex.Message);
    }

    [Fact]
    public async Task GetMoodEntryByIdAsync_AstronautCannotViewAnotherAstronautsEntry()
    {
        var entry = new MoodEntry
        {
            ClientGeneratedId = Guid.NewGuid(),
            AstronautId = 2,
            RecordedAt = DateTime.UtcNow,
            MoodScore = 6,
            StressLevel = 5,
            DominantEmotion = "Fatigued",
            CreatedAt = DateTime.UtcNow
        };
        _context.MoodEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Current user is Astronaut 1
        var result = await _moodService.GetMoodEntryByIdAsync(entry.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMoodHistoryPagedAsync_RespectsDateRangeAndPagination()
    {
        var baseTime = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 8; i++)
        {
            _context.MoodEntries.Add(new MoodEntry
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                RecordedAt = baseTime.AddDays(i),
                MoodScore = 6 + (i % 4),
                StressLevel = 3,
                DominantEmotion = "Calm",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Filter days 1 to 4 (4 entries), page size 2
        var paged = await _moodService.GetMoodHistoryPagedAsync(
            1,
            fromUtc: baseTime.AddDays(1),
            toUtc: baseTime.AddDays(4),
            pageNumber: 1,
            pageSize: 2);

        Assert.Equal(4, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(2, paged.TotalPages);
        Assert.True(paged.HasNextPage);
    }

    [Fact]
    public async Task GetLatestMoodEntryAsync_ReturnsLatestEntry()
    {
        var now = DateTime.UtcNow;
        _context.MoodEntries.AddRange(
            new MoodEntry
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                RecordedAt = now.AddDays(-2),
                MoodScore = 6,
                StressLevel = 4,
                DominantEmotion = "Tired",
                CreatedAt = now.AddDays(-2)
            },
            new MoodEntry
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                RecordedAt = now.AddDays(-1),
                MoodScore = 9,
                StressLevel = 2,
                DominantEmotion = "Energized",
                CreatedAt = now.AddDays(-1)
            }
        );
        await _context.SaveChangesAsync();

        var latest = await _moodService.GetLatestMoodEntryAsync(1);
        Assert.NotNull(latest);
        Assert.Equal(9, latest.MoodScore);
        Assert.Equal("Energized", latest.DominantEmotion);
    }

    [Fact]
    public async Task MonitorCanViewAnyAstronautsMoodEntry()
    {
        var entry = new MoodEntry
        {
            ClientGeneratedId = Guid.NewGuid(),
            AstronautId = 2,
            RecordedAt = DateTime.UtcNow,
            MoodScore = 8,
            StressLevel = 3,
            DominantEmotion = "Calm",
            CreatedAt = DateTime.UtcNow
        };
        _context.MoodEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Switch to Monitor role
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var result = await _moodService.GetMoodEntryByIdAsync(entry.Id);
        Assert.NotNull(result);
        Assert.Equal(2, result.AstronautId);

        var history = await _moodService.GetMoodHistoryPagedAsync(2, null, null, 1, 10);
        Assert.NotNull(history);
        Assert.NotEmpty(history.Items);
    }

    [Fact]
    public async Task RecordMoodAsync_WhenAstronautIsDeactivated_ThrowsInvalidOperationException()
    {
        var astro = await _context.Astronauts.FindAsync(1);
        Assert.NotNull(astro);
        astro.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new CreateMoodEntryRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = DateTime.UtcNow,
            MoodScore = 7,
            StressLevel = 3,
            DominantEmotion = "Calm"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _moodService.RecordMoodAsync(1, request));

        Assert.Contains("Cannot record mood for deactivated astronaut", ex.Message);
    }
}

