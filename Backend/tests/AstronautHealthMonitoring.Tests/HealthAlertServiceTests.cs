using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Auth;
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

public class HealthAlertServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TestCurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IHealthAlertService _healthAlertService;

    public HealthAlertServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"HealthAlertServiceTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        // Ensure Astronauts exist
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
        config.Scan(typeof(AlertMappings).Assembly);
        _mapper = new ServiceMapper(null!, config);

        _healthAlertService = new HealthAlertService(_context, _currentUserService, _mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task TriggerManualAlertAsync_WhenCalledByAstronaut_ScopesToCurrentAstronaut()
    {
        var request = new TriggerManualAlertRequest
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Manual Dizziness Report",
            Message = "Astronaut reported transient dizziness during airlock pressurization.",
            Reason = "Self-reported symptom",
            ActionRequired = "Sit down and rest; monitor vitals."
        };

        var result = await _healthAlertService.TriggerManualAlertAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.AstronautId);
        Assert.Equal(AlertSeverity.MedicalAttention, result.Severity);
        Assert.Equal(AlertStatus.Active, result.Status);
        Assert.Equal("Manual Dizziness Report", result.Title);
    }

    [Fact]
    public async Task TriggerManualAlertAsync_WhenAstronautAttemptsForAnotherAstronaut_ThrowsUnauthorizedAccessException()
    {
        var request = new TriggerManualAlertRequest
        {
            AstronautId = 2, // Calling user is Astronaut 1
            Severity = AlertSeverity.MedicalAttention,
            Title = "Unauthorized Report",
            Message = "Attempting to create alert for astronaut 2"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _healthAlertService.TriggerManualAlertAsync(request));

        Assert.Contains("Astronauts can only trigger manual alerts for themselves", ex.Message);
    }

    [Fact]
    public async Task TriggerManualAlertAsync_WhenCalledByMonitor_CanTargetAnyValidAstronaut()
    {
        // Switch to Monitor role
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;
        _currentUserService.UserId = "monitor-01";

        var request = new TriggerManualAlertRequest
        {
            AstronautId = 2,
            Severity = AlertSeverity.Emergency,
            Title = "Ground Monitor Emergency Override",
            Message = "EVA airlock depressurization anomaly detected on telemetry.",
            EmergencyProcedureId = 1
        };

        var result = await _healthAlertService.TriggerManualAlertAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.AstronautId);
        Assert.Equal(AlertSeverity.Emergency, result.Severity);
        Assert.Equal(1, result.EmergencyProcedureId);
    }

    [Fact]
    public async Task TriggerManualAlertAsync_WhenAstronautIsDeactivated_ThrowsInvalidOperationException()
    {
        var astronaut = await _context.Astronauts.FirstAsync(a => a.Id == 1);
        astronaut.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new TriggerManualAlertRequest
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Deactivated Astronaut Alert",
            Message = "Test alert"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _healthAlertService.TriggerManualAlertAsync(request));

        Assert.Contains("deactivated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TriggerManualAlertAsync_WhenMonitorTargetsDeactivatedAstronaut_ThrowsInvalidOperationException()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var astronaut = await _context.Astronauts.FirstAsync(a => a.Id == 2);
        astronaut.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new TriggerManualAlertRequest
        {
            AstronautId = 2,
            Severity = AlertSeverity.Emergency,
            Title = "Monitor Target Deactivated Alert",
            Message = "Test alert"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _healthAlertService.TriggerManualAlertAsync(request));

        Assert.Contains("deactivated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AcknowledgeAlertAsync_WhenCalledByMonitor_Succeeds_AndUpdatesFields()
    {
        // Arrange: Create active alert
        var alert = new HealthAlert
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Tachypnea",
            Message = "Elevated breathing rate",
            Reason = "Rate breached normal",
            ActionRequired = "Check oxygen line",
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.HealthAlerts.Add(alert);
        await _context.SaveChangesAsync();

        // Switch to Monitor role
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;
        _currentUserService.UserId = "flight-monitor-vance";

        var ackRequest = new AcknowledgeAlertRequest
        {
            ActionTaken = "Spoke with astronaut; oxygen supply confirmed stable."
        };

        // Act
        var result = await _healthAlertService.AcknowledgeAlertAsync(alert.Id, ackRequest, "flight-monitor-vance");

        // Assert
        Assert.Equal(AlertStatus.Acknowledged, result.Status);
        Assert.NotNull(result.AcknowledgedAt);
        Assert.Equal("flight-monitor-vance", result.AcknowledgedBy);
        Assert.Equal("Spoke with astronaut; oxygen supply confirmed stable.", result.ActionTaken);

        var dbAlert = await _context.HealthAlerts.FindAsync(alert.Id);
        Assert.NotNull(dbAlert);
        Assert.Equal(AlertStatus.Acknowledged, dbAlert.Status);
    }

    [Fact]
    public async Task AcknowledgeAlertAsync_WhenCalledByAstronaut_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var alert = new HealthAlert
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Tachypnea",
            Message = "Elevated breathing rate",
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.HealthAlerts.Add(alert);
        await _context.SaveChangesAsync();

        // Calling user is Astronaut
        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = 1;

        var ackRequest = new AcknowledgeAlertRequest { ActionTaken = "Acknowledged by crew" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _healthAlertService.AcknowledgeAlertAsync(alert.Id, ackRequest, "user-astro-1"));

        Assert.Contains("Only flight monitors can acknowledge health alerts", ex.Message);
    }

    [Fact]
    public async Task ResolveAlertAsync_WhenCalledByMonitor_Succeeds_AndSetsResolvedStatus()
    {
        // Arrange: Create alert
        var alert = new HealthAlert
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Heart Rate Spike",
            Message = "Elevated pulse during rest",
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.HealthAlerts.Add(alert);
        await _context.SaveChangesAsync();

        // Switch to Monitor
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var resolveRequest = new ResolveAlertRequest
        {
            ResolutionNotes = "Astronaut completed hydration protocol and heart rate normalized to 72 bpm."
        };

        // Act
        var result = await _healthAlertService.ResolveAlertAsync(alert.Id, resolveRequest, "monitor-01");

        // Assert
        Assert.Equal(AlertStatus.Resolved, result.Status);
        Assert.NotNull(result.AcknowledgedAt);
        Assert.Contains("hydration", result.ActionTaken?.ToLowerInvariant() ?? "");

        var dbAlert = await _context.HealthAlerts.FindAsync(alert.Id);
        Assert.NotNull(dbAlert);
        Assert.Equal(AlertStatus.Resolved, dbAlert.Status);
    }

    [Fact]
    public async Task ResolveAlertAsync_WhenCalledByAstronaut_ThrowsUnauthorizedAccessException()
    {
        var alert = new HealthAlert
        {
            AstronautId = 1,
            Severity = AlertSeverity.MedicalAttention,
            Title = "Heart Rate Spike",
            Message = "Elevated pulse",
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.HealthAlerts.Add(alert);
        await _context.SaveChangesAsync();

        // Calling user is Astronaut
        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = 1;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _healthAlertService.ResolveAlertAsync(alert.Id, new ResolveAlertRequest(), "user-astro-1"));
    }

    [Fact]
    public async Task Astronaut_OnlySeesOwnAlerts()
    {
        // Add alerts for Astronaut 1 and Astronaut 2
        _context.HealthAlerts.AddRange(
            new HealthAlert
            {
                AstronautId = 1,
                Severity = AlertSeverity.MedicalAttention,
                Title = "Astro 1 Alert",
                Message = "Message 1",
                Status = AlertStatus.Active,
                TriggeredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new HealthAlert
            {
                AstronautId = 2,
                Severity = AlertSeverity.Emergency,
                Title = "Astro 2 Alert",
                Message = "Message 2",
                Status = AlertStatus.Active,
                TriggeredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Current user is Astronaut 1
        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = 1;

        // Query paged alerts without astronautId param -> automatically filtered to 1
        var paged = await _healthAlertService.GetAlertsPagedAsync(null, null, null);
        Assert.Single(paged.Items);
        Assert.Equal(1, paged.Items[0].AstronautId);
        Assert.Equal("Astro 1 Alert", paged.Items[0].Title);

        // Querying for Astronaut 2 explicitly throws
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _healthAlertService.GetAlertsPagedAsync(null, null, 2));

        // GetById for Astronaut 2 alert returns null
        var astro2AlertId = _context.HealthAlerts.First(a => a.AstronautId == 2).Id;
        var fetchResult = await _healthAlertService.GetAlertByIdAsync(astro2AlertId);
        Assert.Null(fetchResult);
    }

    [Fact]
    public async Task Monitor_CanFilterByStatusAndSeverityAcrossAllAstronauts()
    {
        _context.HealthAlerts.AddRange(
            new HealthAlert
            {
                AstronautId = 1,
                Severity = AlertSeverity.MedicalAttention,
                Status = AlertStatus.Active,
                Title = "Alert 1",
                Message = "Msg 1",
                TriggeredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new HealthAlert
            {
                AstronautId = 2,
                Severity = AlertSeverity.Emergency,
                Status = AlertStatus.Active,
                Title = "Alert 2",
                Message = "Msg 2",
                TriggeredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new HealthAlert
            {
                AstronautId = 1,
                Severity = AlertSeverity.Emergency,
                Status = AlertStatus.Resolved,
                Title = "Alert 3",
                Message = "Msg 3",
                TriggeredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Switch to Monitor
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        // Filter: Active + Emergency
        var activeEmergency = await _healthAlertService.GetAlertsPagedAsync(AlertStatus.Active, AlertSeverity.Emergency, null);
        Assert.Single(activeEmergency.Items);
        Assert.Equal("Alert 2", activeEmergency.Items[0].Title);
        Assert.Equal(2, activeEmergency.Items[0].AstronautId);
    }
}
