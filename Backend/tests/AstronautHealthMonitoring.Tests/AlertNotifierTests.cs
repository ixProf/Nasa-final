using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.VitalSigns;
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

public class TestAlertNotifier : IAlertNotifier
{
    public List<HealthAlertDto> CreatedNotifications { get; } = new();
    public List<HealthAlertDto> AcknowledgedNotifications { get; } = new();
    public List<HealthAlertDto> ResolvedNotifications { get; } = new();

    public bool ShouldThrow { get; set; } = false;

    public Task NotifyAlertCreatedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("SignalR connection dropped unexpectedly.");
        }
        CreatedNotifications.Add(alert);
        return Task.CompletedTask;
    }

    public Task NotifyAlertAcknowledgedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("SignalR connection dropped unexpectedly.");
        }
        AcknowledgedNotifications.Add(alert);
        return Task.CompletedTask;
    }

    public Task NotifyAlertResolvedAsync(HealthAlertDto alert, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("SignalR connection dropped unexpectedly.");
        }
        ResolvedNotifications.Add(alert);
        return Task.CompletedTask;
    }
}

public class AlertNotifierTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TestCurrentUserService _currentUserService;
    private readonly TestAlertNotifier _notifier;
    private readonly IMapper _mapper;
    private readonly IVitalSignStatusEvaluator _evaluator;
    private readonly IHealthAlertService _healthAlertService;
    private readonly IVitalSignService _vitalSignService;

    public AlertNotifierTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"AlertNotifierTestDb_{Guid.NewGuid()}")
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
            _context.SaveChanges();
        }

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Astronaut,
            AstronautId = 1,
            UserId = "user-astro-1"
        };

        _notifier = new TestAlertNotifier();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(VitalSignMappings).Assembly);
        config.Scan(typeof(AlertMappings).Assembly);
        _mapper = new ServiceMapper(null!, config);

        _evaluator = new VitalSignStatusEvaluator();
        _healthAlertService = new HealthAlertService(_context, _currentUserService, _mapper, _notifier);
        _vitalSignService = new VitalSignService(_context, _currentUserService, _evaluator, _healthAlertService, _mapper, _notifier);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task VitalSignService_WhenReadingIsRoutine_NotifierIsNotCalled()
    {
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1,
            Value = 75, // Normal HR
            RecordedAt = DateTime.UtcNow
        };

        var result = await _vitalSignService.RecordReadingAsync(1, request);

        Assert.Equal(VitalSignStatus.Routine, result.Status);
        Assert.Empty(_notifier.CreatedNotifications);
    }

    [Fact]
    public async Task VitalSignService_WhenReadingIsNonRoutine_CallsNotifierAfterSave()
    {
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1,
            Value = 120, // MedicalAttention
            RecordedAt = DateTime.UtcNow
        };

        var result = await _vitalSignService.RecordReadingAsync(1, request);

        Assert.Equal(VitalSignStatus.MedicalAttention, result.Status);
        Assert.Single(_notifier.CreatedNotifications);
        Assert.Equal(1, _notifier.CreatedNotifications[0].AstronautId);
        Assert.Equal(AlertSeverity.MedicalAttention, _notifier.CreatedNotifications[0].Severity);
    }

    [Fact]
    public async Task VitalSignService_WhenNotifierThrows_SaveStillSucceeds_AndDoesNotThrow()
    {
        _notifier.ShouldThrow = true;

        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1,
            Value = 150, // Emergency
            RecordedAt = DateTime.UtcNow
        };

        // Act: Should NOT throw even though notifier failed
        var result = await _vitalSignService.RecordReadingAsync(1, request);

        // Assert: Reading was successfully saved
        Assert.NotNull(result);
        Assert.Equal(VitalSignStatus.Emergency, result.Status);

        var dbReading = await _context.VitalSignReadings.FindAsync(result.Id);
        Assert.NotNull(dbReading);
    }

    [Fact]
    public async Task HealthAlertService_WhenTriggerManualAlertSucceeds_CallsNotifier()
    {
        var request = new TriggerManualAlertRequest
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Manual Sickness Report",
            Message = "Astronaut reported vestibular adaptation nausea."
        };

        var result = await _healthAlertService.TriggerManualAlertAsync(request);

        Assert.NotNull(result);
        Assert.Single(_notifier.CreatedNotifications);
        Assert.Equal("Manual Sickness Report", _notifier.CreatedNotifications[0].Title);
    }

    [Fact]
    public async Task HealthAlertService_WhenAcknowledgeSucceeds_CallsNotifierAcknowledged()
    {
        var alert = new HealthAlert
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Elevated Heart Rate",
            Message = "Pulse high",
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.HealthAlerts.Add(alert);
        await _context.SaveChangesAsync();

        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var result = await _healthAlertService.AcknowledgeAlertAsync(
            alert.Id,
            new AcknowledgeAlertRequest { ActionTaken = "Monitoring" },
            "monitor-01");

        Assert.Equal(AlertStatus.Acknowledged, result.Status);
        Assert.Single(_notifier.AcknowledgedNotifications);
        Assert.Equal(alert.Id, _notifier.AcknowledgedNotifications[0].Id);
    }

    [Fact]
    public async Task HealthAlertService_WhenResolveSucceeds_CallsNotifierResolved()
    {
        var alert = new HealthAlert
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Elevated Heart Rate",
            Message = "Pulse high",
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.HealthAlerts.Add(alert);
        await _context.SaveChangesAsync();

        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var result = await _healthAlertService.ResolveAlertAsync(
            alert.Id,
            new ResolveAlertRequest { ResolutionNotes = "Telemetry normal" },
            "monitor-01");

        Assert.Equal(AlertStatus.Resolved, result.Status);
        Assert.Single(_notifier.ResolvedNotifications);
        Assert.Equal(alert.Id, _notifier.ResolvedNotifications[0].Id);
    }
}

