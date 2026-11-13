using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using AstronautHealthMonitoring.Validators.Alerts;
using AstronautHealthMonitoring.Validators.Mood;
using AstronautHealthMonitoring.Validators.Sleep;
using AstronautHealthMonitoring.Validators.VitalSigns;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class ControllerAuthorizationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TestCurrentUserService _currentUserService;
    private readonly VitalSignsController _vitalSignsController;
    private readonly HealthAlertsController _healthAlertsController;
    private readonly SleepController _sleepController;
    private readonly MoodController _moodController;

    public ControllerAuthorizationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ControllerAuthTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Ensure Astronaut 1 exists
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
            _context.SaveChanges();
        }

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Astronaut,
            AstronautId = 1,
            UserId = "user-astro-1"
        };

        var config = new TypeAdapterConfig();
        config.Scan(typeof(VitalSignMappings).Assembly);
        config.Scan(typeof(AlertMappings).Assembly);
        config.Scan(typeof(SleepMappings).Assembly);
        config.Scan(typeof(MoodMappings).Assembly);
        var mapper = new ServiceMapper(null!, config);

        var evaluator = new VitalSignStatusEvaluator();
        var alertService = new HealthAlertService(_context, _currentUserService, mapper);
        var vitalSignService = new VitalSignService(_context, _currentUserService, evaluator, alertService, mapper);
        var sleepService = new SleepService(_context, _currentUserService, mapper);
        var moodService = new MoodService(_context, _currentUserService, mapper);

        var createReadingValidator = new CreateVitalSignReadingRequestValidator();
        var manualAlertValidator = new TriggerManualAlertRequestValidator();
        var ackAlertValidator = new AcknowledgeAlertRequestValidator();
        var resolveAlertValidator = new ResolveAlertRequestValidator();
        var sleepValidator = new CreateSleepRecordRequestValidator();
        var moodValidator = new CreateMoodEntryRequestValidator();

        _vitalSignsController = new VitalSignsController(vitalSignService, _currentUserService, createReadingValidator);
        _vitalSignsController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        _healthAlertsController = new HealthAlertsController(alertService, _currentUserService, manualAlertValidator, ackAlertValidator, resolveAlertValidator);
        _healthAlertsController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        _sleepController = new SleepController(sleepService, _currentUserService, sleepValidator);
        _sleepController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        _moodController = new MoodController(moodService, _currentUserService, moodValidator);
        _moodController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task VitalSignsController_RecordReading_Valid_Returns201CreatedWithLocation()
    {
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1,
            Value = 72,
            RecordedAt = DateTime.UtcNow
        };

        var actionResult = await _vitalSignsController.RecordReading(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(_vitalSignsController.GetById), createdResult.ActionName);
        var returnedDto = Assert.IsType<VitalSignReadingDto>(createdResult.Value);
        Assert.Equal(72, returnedDto.Value);
    }

    [Fact]
    public async Task VitalSignsController_RecordReading_Invalid_Returns400BadRequest()
    {
        // ClientGeneratedId is empty (invalid)
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.Empty,
            VitalSignTypeId = 1,
            Value = 72,
            RecordedAt = DateTime.UtcNow
        };

        var actionResult = await _vitalSignsController.RecordReading(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task VitalSignsController_GetById_WhenNotFound_Returns404NotFound()
    {
        var actionResult = await _vitalSignsController.GetById(99999, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task VitalSignsController_GetLatest_Returns200OK()
    {
        var actionResult = await _vitalSignsController.GetLatest(null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task HealthAlertsController_TriggerManualAlert_Valid_Returns201Created()
    {
        var request = new TriggerManualAlertRequest
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Manual Dizziness",
            Message = "Crew reported dizziness in airlock"
        };

        var actionResult = await _healthAlertsController.TriggerManualAlert(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        var returnedDto = Assert.IsType<HealthAlertDto>(createdResult.Value);
        Assert.Equal("Manual Dizziness", returnedDto.Title);
    }

    [Fact]
    public async Task HealthAlertsController_TriggerManualAlert_Invalid_Returns400BadRequest()
    {
        // Missing title and message
        var request = new TriggerManualAlertRequest
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "",
            Message = ""
        };

        var actionResult = await _healthAlertsController.TriggerManualAlert(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task HealthAlertsController_TriggerManualAlert_WhenDeactivated_Returns409Conflict()
    {
        var astronaut = await _context.Astronauts.FirstAsync(a => a.Id == 1);
        astronaut.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new TriggerManualAlertRequest
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Manual Dizziness",
            Message = "Crew reported dizziness in airlock"
        };

        var actionResult = await _healthAlertsController.TriggerManualAlert(request, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    [Fact]
    public async Task HealthAlertsController_AcknowledgeAlert_WhenMonitor_Returns200OK()
    {
        // Create an alert
        var alert = new HealthAlert
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "High Heart Rate",
            Message = "Pulse reached 120 bpm",
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.HealthAlerts.Add(alert);
        await _context.SaveChangesAsync();

        // Switch to Monitor
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;
        _currentUserService.UserId = "monitor-alex";

        var actionResult = await _healthAlertsController.AcknowledgeAlert(
            alert.Id,
            new AcknowledgeAlertRequest { ActionTaken = "Checked telemetry" },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var dto = Assert.IsType<HealthAlertDto>(okResult.Value);
        Assert.Equal(AlertStatus.Acknowledged, dto.Status);
    }

    [Fact]
    public async Task HealthAlertsController_ResolveAlert_WhenMonitor_Returns200OK()
    {
        // Create an alert
        var alert = new HealthAlert
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Elevated Blood Glucose",
            Message = "Glucose 130 mg/dL",
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.HealthAlerts.Add(alert);
        await _context.SaveChangesAsync();

        // Switch to Monitor
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var actionResult = await _healthAlertsController.ResolveAlert(
            alert.Id,
            new ResolveAlertRequest { ResolutionNotes = "Diet adjusted" },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var dto = Assert.IsType<HealthAlertDto>(okResult.Value);
        Assert.Equal(AlertStatus.Resolved, dto.Status);
    }

    [Fact]
    public async Task SleepController_RecordSleep_Valid_Returns201CreatedWithLocation()
    {
        var now = DateTime.UtcNow;
        var request = new CreateSleepRecordRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = now,
            StartTime = now.AddHours(-8),
            EndTime = now,
            Quality = SleepQuality.Good,
            DeepSleepMinutes = 120,
            RemSleepMinutes = 90,
            LightSleepMinutes = 240,
            AwakeMinutes = 30,
            SleepEfficiencyPercentage = 93.75
        };

        var actionResult = await _sleepController.RecordSleep(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(_sleepController.GetById), createdResult.ActionName);
        var returnedDto = Assert.IsType<SleepRecordDto>(createdResult.Value);
        Assert.Equal(8.0, returnedDto.DurationHours);
    }

    [Fact]
    public async Task SleepController_RecordSleep_Invalid_Returns400BadRequest()
    {
        var request = new CreateSleepRecordRequest
        {
            ClientGeneratedId = Guid.Empty, // Invalid
            RecordedAt = DateTime.UtcNow,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(-1) // End before start
        };

        var actionResult = await _sleepController.RecordSleep(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task SleepController_GetById_WhenNotFound_Returns404NotFound()
    {
        var actionResult = await _sleepController.GetById(99999, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task MoodController_RecordMood_Valid_Returns201CreatedWithLocation()
    {
        var request = new CreateMoodEntryRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            RecordedAt = DateTime.UtcNow,
            MoodScore = 8,
            StressLevel = 3,
            DominantEmotion = "Focused"
        };

        var actionResult = await _moodController.RecordMood(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(_moodController.GetById), createdResult.ActionName);
        var returnedDto = Assert.IsType<MoodEntryDto>(createdResult.Value);
        Assert.Equal("Focused", returnedDto.DominantEmotion);
    }

    [Fact]
    public async Task MoodController_RecordMood_Invalid_Returns400BadRequest()
    {
        var request = new CreateMoodEntryRequest
        {
            ClientGeneratedId = Guid.Empty,
            RecordedAt = DateTime.UtcNow,
            MoodScore = 99, // Out of range 1-10
            StressLevel = 0,
            DominantEmotion = ""
        };

        var actionResult = await _moodController.RecordMood(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task MoodController_GetById_WhenNotFound_Returns404NotFound()
    {
        var actionResult = await _moodController.GetById(99999, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }
}

