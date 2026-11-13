using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.DTOs.Sync;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class SyncServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly TestCurrentUserService _currentUserService;
    private readonly IVitalSignStatusEvaluator _evaluator;
    private readonly IHealthAlertService _healthAlertService;
    private readonly IVitalSignService _vitalSignService;
    private readonly ISleepService _sleepService;
    private readonly IMoodService _moodService;
    private readonly IChatService _chatService;
    private readonly IConfiguration _configuration;
    private readonly SyncService _syncService;

    public SyncServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"SyncServiceDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _context.Astronauts.AddRange(
            new Astronaut
            {
                Id = 1,
                AstronautCode = "AST-01",
                FirstName = "Major",
                LastName = "Tom",
                FullName = "Major Tom",
                DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                BloodType = "O+",
                HeightCm = 175,
                WeightKg = 70,
                IsActive = true
            },
            new Astronaut
            {
                Id = 2,
                AstronautCode = "AST-02",
                FirstName = "Sarah",
                LastName = "Connor",
                FullName = "Sarah Connor",
                DateOfBirth = new DateTime(1985, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Female",
                BloodType = "A+",
                HeightCm = 168,
                WeightKg = 62,
                IsActive = true
            }
        );
        _context.SaveChanges();

        var config = new TypeAdapterConfig();
        new VitalSignMappings().Register(config);
        new AlertMappings().Register(config);
        new SleepMappings().Register(config);
        new MoodMappings().Register(config);
        new ChatMappings().Register(config);
        _mapper = new Mapper(config);

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Astronaut,
            UserId = "astro-01",
            AstronautId = 1,
            FullName = "Major Tom",
            IsAuthenticated = true
        };

        _evaluator = new VitalSignStatusEvaluator();
        _healthAlertService = new HealthAlertService(
            _context,
            _currentUserService,
            _mapper);

        _vitalSignService = new VitalSignService(
            _context,
            _currentUserService,
            _evaluator,
            _healthAlertService,
            _mapper);

        var configValues = new Dictionary<string, string?>
        {
            { "Sync:MaxBatchSize", "200" },
            { "Sync:FutureToleranceMinutes", "5" },
            { "Sync:MaxAgeDays", "30" }
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        _sleepService = new SleepService(_context, _currentUserService, _mapper);
        _moodService = new MoodService(_context, _currentUserService, _mapper);
        _chatService = new ChatService(_context, _mapper, NullLogger<ChatService>.Instance);

        _syncService = new SyncService(
            _context,
            _vitalSignService,
            _sleepService,
            _moodService,
            _chatService,
            _configuration,
            NullLogger<SyncService>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ProcessSyncBatchAsync_FullSuccessBatch_ProcessesAllItems()
    {
        var now = DateTime.UtcNow.AddMinutes(-10);

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow,
            VitalSignReadings = new List<CreateVitalSignReadingRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    VitalSignTypeId = 1, // Heart rate
                    Value = 72,
                    MeasurementMethod = "Continuous Telemetry",
                    SensorLocation = "Chest Band",
                    RecordedAt = now
                }
            },
            SleepRecords = new List<CreateSleepRecordRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    RecordedAt = now,
                    StartTime = now.AddHours(-8),
                    EndTime = now,
                    DeepSleepMinutes = 120,
                    RemSleepMinutes = 100,
                    LightSleepMinutes = 240,
                    AwakeMinutes = 20,
                    Quality = SleepQuality.Good
                }
            },
            MoodEntries = new List<CreateMoodEntryRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    RecordedAt = now,
                    MoodScore = 4,
                    StressLevel = 2,
                    DominantEmotion = "Calm"
                }
            },
            ChatMessages = new List<SendChatMessageRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    Content = "Morning sync test message.",
                    RecordedAt = now
                }
            }
        };

        var response = await _syncService.ProcessSyncBatchAsync(1, request, "astro-01");

        Assert.True(response.Success);
        Assert.Equal(1, response.VitalSignReadingsProcessed);
        Assert.Equal(1, response.SleepRecordsProcessed);
        Assert.Equal(1, response.MoodEntriesProcessed);
        Assert.Equal(1, response.ChatMessagesProcessed);
        Assert.Equal(0, response.DuplicatesIgnoredCount);
        Assert.Empty(response.Errors);
        Assert.Equal(4, response.ItemResults.Count);
        Assert.All(response.ItemResults, r => Assert.Equal(SyncItemStatus.Accepted, r.Status));
    }

    [Fact]
    public async Task ProcessSyncBatchAsync_DuplicateResend_IsIdempotent()
    {
        var now = DateTime.UtcNow.AddMinutes(-5);
        var vitalId = Guid.NewGuid();

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow,
            VitalSignReadings = new List<CreateVitalSignReadingRequest>
            {
                new()
                {
                    ClientGeneratedId = vitalId,
                    VitalSignTypeId = 1,
                    Value = 75,
                    MeasurementMethod = "Chest Sensor",
                    SensorLocation = "Chest",
                    RecordedAt = now
                }
            }
        };

        var first = await _syncService.ProcessSyncBatchAsync(1, request, "astro-01");
        Assert.True(first.Success);
        Assert.Equal(1, first.VitalSignReadingsProcessed);
        Assert.Equal(0, first.DuplicatesIgnoredCount);

        var second = await _syncService.ProcessSyncBatchAsync(1, request, "astro-01");
        Assert.True(second.Success);
        Assert.Equal(0, second.VitalSignReadingsProcessed);
        Assert.Equal(1, second.DuplicatesIgnoredCount);
        Assert.Equal(SyncItemStatus.Duplicate, second.ItemResults[0].Status);

        var dbCount = await _context.VitalSignReadings.CountAsync(v => v.ClientGeneratedId == vitalId);
        Assert.Equal(1, dbCount);
    }

    [Fact]
    public async Task ProcessSyncBatchAsync_MixedBatch_BadItemDoesNotFailWholeBatch()
    {
        var now = DateTime.UtcNow.AddMinutes(-10);

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow,
            VitalSignReadings = new List<CreateVitalSignReadingRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    VitalSignTypeId = 9999, // Invalid vital sign type id
                    Value = 72,
                    MeasurementMethod = "Sensor",
                    SensorLocation = "Chest",
                    RecordedAt = now
                }
            },
            MoodEntries = new List<CreateMoodEntryRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    RecordedAt = now,
                    MoodScore = 4,
                    StressLevel = 2,
                    DominantEmotion = "Focused"
                }
            }
        };

        var response = await _syncService.ProcessSyncBatchAsync(1, request, "astro-01");

        Assert.False(response.Success); // Contains errors
        Assert.Equal(0, response.VitalSignReadingsProcessed);
        Assert.Equal(1, response.MoodEntriesProcessed); // Mood succeeded!
        Assert.NotEmpty(response.Errors);

        var rejected = response.ItemResults.First(r => r.ItemType == "VitalSign");
        Assert.Equal(SyncItemStatus.Rejected, rejected.Status);

        var accepted = response.ItemResults.First(r => r.ItemType == "Mood");
        Assert.Equal(SyncItemStatus.Accepted, accepted.Status);
    }

    [Fact]
    public async Task ProcessSyncBatchAsync_OversizedBatch_ThrowsArgumentException()
    {
        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow
        };

        // Add 201 items
        for (int i = 0; i < 201; i++)
        {
            request.MoodEntries.Add(new CreateMoodEntryRequest
            {
                ClientGeneratedId = Guid.NewGuid(),
                RecordedAt = DateTime.UtcNow.AddMinutes(-1),
                MoodScore = 3,
                StressLevel = 3,
                DominantEmotion = "Tired"
            });
        }

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _syncService.ProcessSyncBatchAsync(1, request, "astro-01"));

        Assert.Contains("exceeds maximum allowed size", ex.Message);
    }

    [Fact]
    public async Task ProcessSyncBatchAsync_FutureTimestamp_RejectsFutureItemWithReason()
    {
        var futureTime = DateTime.UtcNow.AddMinutes(30); // Exceeds 5 min tolerance

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow,
            VitalSignReadings = new List<CreateVitalSignReadingRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    VitalSignTypeId = 1,
                    Value = 70,
                    MeasurementMethod = "Telemetry",
                    SensorLocation = "Wrist",
                    RecordedAt = futureTime
                }
            }
        };

        var response = await _syncService.ProcessSyncBatchAsync(1, request, "astro-01");

        Assert.False(response.Success);
        Assert.Equal(0, response.VitalSignReadingsProcessed);
        Assert.Single(response.ItemResults);
        Assert.Equal(SyncItemStatus.Rejected, response.ItemResults[0].Status);
        Assert.Contains("future", response.ItemResults[0].Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessSyncBatchAsync_CrossAstronautItem_RejectsCrossAstronautItem()
    {
        var now = DateTime.UtcNow.AddMinutes(-2);

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow,
            ChatMessages = new List<SendChatMessageRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    AstronautId = 2, // Calling for Astronaut 1
                    Content = "Cross astronaut message",
                    RecordedAt = now
                }
            }
        };

        var response = await _syncService.ProcessSyncBatchAsync(1, request, "astro-01");

        Assert.False(response.Success);
        Assert.Equal(0, response.ChatMessagesProcessed);
        Assert.Single(response.ItemResults);
        Assert.Equal(SyncItemStatus.Rejected, response.ItemResults[0].Status);
        Assert.Contains("Cross-astronaut", response.ItemResults[0].Reason!);
    }

    [Fact]
    public async Task ProcessSyncBatchAsync_DeactivatedAstronaut_ThrowsInvalidOperationException()
    {
        var astronaut = await _context.Astronauts.FirstAsync(a => a.Id == 1);
        astronaut.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _syncService.ProcessSyncBatchAsync(1, request, "astro-01"));

        Assert.Contains("deactivated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessSyncBatchAsync_EmergencyVital_TriggersHealthAlert()
    {
        var now = DateTime.UtcNow.AddMinutes(-2);

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow,
            VitalSignReadings = new List<CreateVitalSignReadingRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    VitalSignTypeId = 1, // Heart rate
                    Value = 180, // EMERGENCY (Normal max is 100, Emergency > 140)
                    MeasurementMethod = "Continuous Telemetry",
                    SensorLocation = "Chest Band",
                    RecordedAt = now
                }
            }
        };

        var response = await _syncService.ProcessSyncBatchAsync(1, request, "astro-01");

        Assert.True(response.Success);
        Assert.Equal(1, response.VitalSignReadingsProcessed);

        // Assert emergency alert was generated in database
        var alert = await _context.HealthAlerts.FirstOrDefaultAsync(a => a.AstronautId == 1 && a.Severity == AlertSeverity.Emergency);
        Assert.NotNull(alert);
        Assert.Equal(AlertStatus.Active, alert.Status);
    }
}
