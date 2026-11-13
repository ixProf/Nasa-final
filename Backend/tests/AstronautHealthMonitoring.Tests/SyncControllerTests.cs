using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.DTOs.Sync;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class SyncControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly TestCurrentUserService _currentUserService;
    private readonly SyncService _syncService;
    private readonly SyncController _controller;

    public SyncControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"SyncControllerDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _context.Astronauts.Add(new Astronaut
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
        });
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

        var configValues = new Dictionary<string, string?>
        {
            { "Sync:MaxBatchSize", "200" },
            { "Sync:FutureToleranceMinutes", "5" },
            { "Sync:MaxAgeDays", "30" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        var evaluator = new VitalSignStatusEvaluator();
        var alertService = new HealthAlertService(_context, _currentUserService, _mapper);
        var vitalSignService = new VitalSignService(_context, _currentUserService, evaluator, alertService, _mapper);
        var sleepService = new SleepService(_context, _currentUserService, _mapper);
        var moodService = new MoodService(_context, _currentUserService, _mapper);
        var chatService = new ChatService(_context, _mapper, NullLogger<ChatService>.Instance);

        _syncService = new SyncService(
            _context,
            vitalSignService,
            sleepService,
            moodService,
            chatService,
            configuration,
            NullLogger<SyncService>.Instance);

        _controller = new SyncController(_syncService, _currentUserService);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task SyncBatch_AstronautCalling_Succeeds()
    {
        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow,
            MoodEntries = new List<CreateMoodEntryRequest>
            {
                new()
                {
                    ClientGeneratedId = Guid.NewGuid(),
                    RecordedAt = DateTime.UtcNow.AddMinutes(-5),
                    MoodScore = 4,
                    StressLevel = 2,
                    DominantEmotion = "Focused"
                }
            }
        };

        var result = await _controller.SyncBatch(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SyncBatchResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.MoodEntriesProcessed);
    }

    [Fact]
    public void SyncBatch_Has_AstronautOnly_Policy()
    {
        var method = typeof(SyncController).GetMethod(nameof(SyncController.SyncBatch));
        Assert.NotNull(method);
        var authAttr = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>(method);
        Assert.NotNull(authAttr);
        Assert.Equal(AuthPolicies.AstronautOnly, authAttr.Policy);
    }

    [Fact]
    public async Task SyncBatch_MonitorCalling_ReturnsForbidden()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow
        };

        var result = await _controller.SyncBatch(request);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task SyncBatch_DeactivatedAstronaut_ReturnsConflict()
    {
        var astronaut = await _context.Astronauts.FirstAsync(a => a.Id == 1);
        astronaut.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow
        };

        var result = await _controller.SyncBatch(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task SyncBatch_OversizedBatch_ReturnsBadRequest()
    {
        var request = new SyncBatchRequest
        {
            DeviceSyncTime = DateTime.UtcNow
        };

        for (int i = 0; i < 201; i++)
        {
            request.MoodEntries.Add(new CreateMoodEntryRequest
            {
                ClientGeneratedId = Guid.NewGuid(),
                RecordedAt = DateTime.UtcNow.AddMinutes(-1),
                MoodScore = 3,
                StressLevel = 2,
                DominantEmotion = "Calm"
            });
        }

        var result = await _controller.SyncBatch(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }
}
