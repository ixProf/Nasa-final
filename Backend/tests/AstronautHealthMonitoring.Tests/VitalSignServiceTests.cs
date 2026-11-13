using AstronautHealthMonitoring.Data;
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

public class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "test-astro-01";
    public string? Role { get; set; } = UserRoles.Astronaut;
    public int? AstronautId { get; set; } = 1;
    public string? FullName { get; set; } = "Alex Vance";
    public bool IsAuthenticated { get; set; } = true;
    public bool IsInRole(string role) => Role == role;
}

public class VitalSignServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TestCurrentUserService _currentUserService;
    private readonly IVitalSignStatusEvaluator _evaluator;
    private readonly IMapper _mapper;
    private readonly IHealthAlertService _healthAlertService;
    private readonly IVitalSignService _vitalSignService;

    public VitalSignServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"VitalSignServiceTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated(); // Seeds reference ranges, procedures, etc.

        // Ensure Astronaut 1 and 2 exist in test database
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
        config.Scan(typeof(VitalSignMappings).Assembly);
        _mapper = new ServiceMapper(null!, config);

        _evaluator = new VitalSignStatusEvaluator();
        _healthAlertService = new HealthAlertService(_context, _currentUserService, _mapper);
        _vitalSignService = new VitalSignService(_context, _currentUserService, _evaluator, _healthAlertService, _mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task RecordReadingAsync_WhenRoutineHeartRate_SavesReading_AndCreatesNoAlert()
    {
        // Arrange: HR 75 bpm is within normal range [60, 100]
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1, // Heart Rate
            Value = 75,
            MeasurementMethod = "Wearable Optical Sensor",
            SensorLocation = "Left Wrist",
            BodyPosition = BodyPosition.MicrogravityFloating,
            ActivityContext = ActivityContext.Resting,
            DataQuality = DataQuality.Valid,
            RecordedAt = DateTime.UtcNow
        };

        // Act
        var result = await _vitalSignService.RecordReadingAsync(1, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.AstronautId);
        Assert.Equal(75, result.Value);
        Assert.Equal(VitalSignStatus.Routine, result.Status);

        var savedReading = await _context.VitalSignReadings.FirstOrDefaultAsync(r => r.Id == result.Id);
        Assert.NotNull(savedReading);
        Assert.Equal(VitalSignStatus.Routine, savedReading.Status);

        // Verify NO health alert was created
        var alerts = await _context.HealthAlerts.Where(a => a.VitalSignReadingId == result.Id).ToListAsync();
        Assert.Empty(alerts);
    }

    [Fact]
    public async Task RecordReadingAsync_WhenMedicalAttentionHeartRate_CreatesMedicalAttentionAlert()
    {
        // Arrange: HR 110 bpm is outside normal [60, 100] but below EmergencyAbove (140)
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1, // Heart Rate
            Value = 110,
            MeasurementMethod = "Wearable Optical Sensor",
            SensorLocation = "Chest Patch",
            BodyPosition = BodyPosition.Sitting,
            ActivityContext = ActivityContext.Resting,
            DataQuality = DataQuality.Valid,
            RecordedAt = DateTime.UtcNow
        };

        // Act
        var result = await _vitalSignService.RecordReadingAsync(1, request);

        // Assert
        Assert.Equal(VitalSignStatus.MedicalAttention, result.Status);

        var alert = await _context.HealthAlerts.FirstOrDefaultAsync(a => a.VitalSignReadingId == result.Id);
        Assert.NotNull(alert);
        Assert.Equal(1, alert.AstronautId);
        Assert.Equal(AlertSeverity.MedicalAttention, alert.Severity);
        Assert.Equal(AlertStatus.Active, alert.Status);
        Assert.Contains("Heart Rate", alert.Title);
        Assert.Null(alert.EmergencyProcedureId);
    }

    [Fact]
    public async Task RecordReadingAsync_WhenEmergencySpO2_CreatesEmergencyAlert_AndLinksHypoxiaProcedure()
    {
        // Arrange: SpO2 82% is below EmergencyBelow (85%)
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 3, // SpO2
            Value = 82,
            MeasurementMethod = "Pulse Oximeter",
            SensorLocation = "Left Index Finger",
            BodyPosition = BodyPosition.MicrogravityFloating,
            ActivityContext = ActivityContext.Resting,
            DataQuality = DataQuality.Valid,
            RecordedAt = DateTime.UtcNow
        };

        // Act
        var result = await _vitalSignService.RecordReadingAsync(1, request);

        // Assert
        Assert.Equal(VitalSignStatus.Emergency, result.Status);

        var alert = await _context.HealthAlerts
            .Include(a => a.EmergencyProcedure)
            .FirstOrDefaultAsync(a => a.VitalSignReadingId == result.Id);

        Assert.NotNull(alert);
        Assert.Equal(AlertSeverity.Emergency, alert.Severity);
        Assert.Equal(AlertStatus.Active, alert.Status);
        Assert.NotNull(alert.EmergencyProcedure);
        Assert.Equal("EP-HYPOXIA-01", alert.EmergencyProcedure.Code);
    }

    [Fact]
    public async Task RecordReadingAsync_WhenEmergencyRespiratoryRate_CreatesEmergencyAlert_AndLinksCO2Procedure()
    {
        // Arrange: Respiratory Rate 35 is above EmergencyAbove (30)
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 6, // RESP
            Value = 35,
            MeasurementMethod = "Chest Impedance Sensor",
            SensorLocation = "Chest Band",
            BodyPosition = BodyPosition.MicrogravityFloating,
            ActivityContext = ActivityContext.Resting,
            DataQuality = DataQuality.Valid,
            RecordedAt = DateTime.UtcNow
        };

        // Act
        var result = await _vitalSignService.RecordReadingAsync(1, request);

        // Assert
        Assert.Equal(VitalSignStatus.Emergency, result.Status);

        var alert = await _context.HealthAlerts
            .Include(a => a.EmergencyProcedure)
            .FirstOrDefaultAsync(a => a.VitalSignReadingId == result.Id);

        Assert.NotNull(alert);
        Assert.Equal(AlertSeverity.Emergency, alert.Severity);
        Assert.NotNull(alert.EmergencyProcedure);
        Assert.Equal("EP-CO2-02", alert.EmergencyProcedure.Code);
    }

    [Fact]
    public async Task RecordReadingAsync_WhenAstronautAttemptsToWriteForAnotherAstronaut_ThrowsUnauthorizedAccessException()
    {
        // Arrange: Current user is Astronaut 1, but attempts to write for Astronaut 2
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1,
            Value = 75,
            RecordedAt = DateTime.UtcNow
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _vitalSignService.RecordReadingAsync(2, request));

        Assert.Contains("Astronauts can only record vital signs for themselves", ex.Message);
    }

    [Fact]
    public async Task RecordReadingAsync_WhenMonitorAttemptsToCreateReading_ThrowsUnauthorizedAccessException()
    {
        // Arrange: Current user is Monitor
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1,
            Value = 75,
            RecordedAt = DateTime.UtcNow
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _vitalSignService.RecordReadingAsync(1, request));

        Assert.Contains("Flight monitors cannot create vital sign readings", ex.Message);
    }

    [Fact]
    public async Task RecordReadingAsync_WithPairedMetricBloodPressure_EvaluatesCorrectly()
    {
        // Blood pressure: Normal 90-120 systolic, 60-80 diastolic
        // Test 1: 115/75 -> Routine
        var requestRoutine = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 2, // BP
            Value = 115,
            SecondaryValue = 75,
            RecordedAt = DateTime.UtcNow
        };
        var resRoutine = await _vitalSignService.RecordReadingAsync(1, requestRoutine);
        Assert.Equal(VitalSignStatus.Routine, resRoutine.Status);

        // Test 2: 135/85 -> MedicalAttention (above normal, below emergency 160/100)
        var requestAttention = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 2,
            Value = 135,
            SecondaryValue = 85,
            RecordedAt = DateTime.UtcNow
        };
        var resAttention = await _vitalSignService.RecordReadingAsync(1, requestAttention);
        Assert.Equal(VitalSignStatus.MedicalAttention, resAttention.Status);

        // Test 3: 170/75 -> Emergency (Systolic > 160)
        var requestEmergency = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 2,
            Value = 170,
            SecondaryValue = 75,
            RecordedAt = DateTime.UtcNow
        };
        var resEmergency = await _vitalSignService.RecordReadingAsync(1, requestEmergency);
        Assert.Equal(VitalSignStatus.Emergency, resEmergency.Status);
    }

    [Fact]
    public async Task RecordReadingAsync_WhenDuplicateClientGeneratedId_ReturnsExistingReading_WithoutDuplicateAlert()
    {
        // Arrange
        var clientGenId = Guid.NewGuid();
        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = clientGenId,
            VitalSignTypeId = 1,
            Value = 115, // Medical attention
            RecordedAt = DateTime.UtcNow
        };

        // Act: First call
        var firstResult = await _vitalSignService.RecordReadingAsync(1, request);

        // Act: Second call with exact same ClientGeneratedId
        var secondResult = await _vitalSignService.RecordReadingAsync(1, request);

        // Assert
        Assert.Equal(firstResult.Id, secondResult.Id);

        // Verify only 1 reading and only 1 alert in DB
        var totalReadings = await _context.VitalSignReadings.CountAsync(r => r.ClientGeneratedId == clientGenId);
        Assert.Equal(1, totalReadings);

        var totalAlerts = await _context.HealthAlerts.CountAsync(a => a.VitalSignReadingId == firstResult.Id);
        Assert.Equal(1, totalAlerts);
    }

    [Fact]
    public async Task GetLatestReadingsPerMetricAsync_ReturnsSingleMostRecentReadingPerType()
    {
        var now = DateTime.UtcNow;

        // Add 2 readings for HR (type 1) and 1 for Temp (type 4)
        _context.VitalSignReadings.AddRange(
            new VitalSignReading
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                VitalSignTypeId = 1,
                Value = 70,
                RecordedAt = now.AddMinutes(-30),
                CreatedAt = now.AddMinutes(-30)
            },
            new VitalSignReading
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                VitalSignTypeId = 1,
                Value = 82, // Latest HR
                RecordedAt = now.AddMinutes(-5),
                CreatedAt = now.AddMinutes(-5)
            },
            new VitalSignReading
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                VitalSignTypeId = 4, // Temp
                Value = 36.8,
                RecordedAt = now.AddMinutes(-10),
                CreatedAt = now.AddMinutes(-10)
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var latest = await _vitalSignService.GetLatestReadingsPerMetricAsync(1);

        // Assert
        Assert.Equal(2, latest.Count);
        var hrReading = latest.FirstOrDefault(r => r.VitalSignTypeId == 1);
        Assert.NotNull(hrReading);
        Assert.Equal(82, hrReading.Value);

        var tempReading = latest.FirstOrDefault(r => r.VitalSignTypeId == 4);
        Assert.NotNull(tempReading);
        Assert.Equal(36.8, tempReading.Value);
    }

    [Fact]
    public async Task GetHistoryAsync_AppliesDateRangeAndPagination()
    {
        var baseTime = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < 15; i++)
        {
            _context.VitalSignReadings.Add(new VitalSignReading
            {
                ClientGeneratedId = Guid.NewGuid(),
                AstronautId = 1,
                VitalSignTypeId = 1,
                Value = 60 + i,
                RecordedAt = baseTime.AddHours(i),
                CreatedAt = baseTime.AddHours(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act: Filter to hours 5 to 10 (6 readings total), page size 4, page 1
        var pagedResult = await _vitalSignService.GetHistoryAsync(
            1,
            fromUtc: baseTime.AddHours(5),
            toUtc: baseTime.AddHours(10),
            pageNumber: 1,
            pageSize: 4);

        // Assert
        Assert.Equal(6, pagedResult.TotalCount);
        Assert.Equal(4, pagedResult.Items.Count);
        Assert.Equal(1, pagedResult.PageNumber);
        Assert.Equal(2, pagedResult.TotalPages);
        Assert.True(pagedResult.HasNextPage);
        Assert.False(pagedResult.HasPreviousPage);
    }

    [Fact]
    public async Task AstronautCannotViewAnotherAstronautsData()
    {
        // Current user is Astronaut 1; attempt to query Astronaut 2's latest readings
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _vitalSignService.GetLatestReadingsPerMetricAsync(2));

        // Attempt to query Astronaut 2's history
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _vitalSignService.GetHistoryAsync(2, null, null, 1, 20));
    }

    [Fact]
    public async Task MonitorCanViewAnyAstronautsData()
    {
        // Switch user to Monitor
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var latest = await _vitalSignService.GetLatestReadingsPerMetricAsync(1);
        Assert.NotNull(latest);

        var history = await _vitalSignService.GetHistoryAsync(2, null, null, 1, 20);
        Assert.NotNull(history);
    }

    [Fact]
    public async Task RecordReadingAsync_WhenAstronautIsDeactivated_ThrowsInvalidOperationException()
    {
        var astro = await _context.Astronauts.FindAsync(1);
        Assert.NotNull(astro);
        astro.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new CreateVitalSignReadingRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            VitalSignTypeId = 1,
            Value = 75,
            MeasurementMethod = "Continuous Telemetry",
            SensorLocation = "Chest Band",
            RecordedAt = DateTime.UtcNow
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _vitalSignService.RecordReadingAsync(1, request));

        Assert.Contains("Cannot record vital signs for deactivated astronaut", ex.Message);
    }
}

