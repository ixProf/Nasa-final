using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class TestHealthAlertService : IHealthAlertService
{
    public List<TriggerManualAlertRequest> TriggeredManualAlerts { get; } = new();

    public Task<HealthAlertDto> TriggerManualAlertAsync(TriggerManualAlertRequest request, CancellationToken cancellationToken = default)
    {
        TriggeredManualAlerts.Add(request);
        return Task.FromResult(new HealthAlertDto
        {
            Id = 999,
            AstronautId = request.AstronautId,
            Severity = request.Severity,
            Title = request.Title,
            Message = request.Message,
            Status = AlertStatus.Active,
            TriggeredAt = DateTime.UtcNow
        });
    }

    public Task<HealthAlertDto> CreateAlertFromReadingAsync(VitalSignReading reading, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<PagedResult<HealthAlertDto>> GetAlertsPagedAsync(AlertStatus? status, AlertSeverity? severity, int? astronautId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<HealthAlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<HealthAlertDto>> GetAlertsForAstronautAsync(int astronautId, bool activeOnly = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<HealthAlertDto?> GetAlertByIdAsync(int alertId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<HealthAlertDto> AcknowledgeAlertAsync(int alertId, AcknowledgeAlertRequest request, string monitorUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<HealthAlertDto> ResolveAlertAsync(int alertId, ResolveAlertRequest? request, string monitorUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

public class SymptomGuidanceServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly TestHealthAlertService _alertService;
    private readonly SymptomGuidanceService _service;

    public SymptomGuidanceServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"SymptomGuidanceDb_{Guid.NewGuid()}")
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
        new SymptomGuidanceMappings().Register(config);
        _mapper = new Mapper(config);

        _alertService = new TestHealthAlertService();

        _service = new SymptomGuidanceService(
            _context,
            _alertService,
            _mapper,
            NullLogger<SymptomGuidanceService>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllSymptomsAsync_ReturnsCatalogFromDatabase()
    {
        var symptoms = await _service.GetAllSymptomsAsync();

        Assert.NotEmpty(symptoms);
        Assert.Contains(symptoms, s => s.Code == "CHEST_PAIN" && s.IsRedFlag);
        Assert.Contains(symptoms, s => s.Code == "DIZZINESS" && !s.IsRedFlag);
    }

    [Fact]
    public async Task GetAllConditionsAsync_WithSearch_ReturnsFilteredConditions()
    {
        var allConditions = await _service.GetAllConditionsAsync();
        Assert.NotEmpty(allConditions);

        var filtered = await _service.GetAllConditionsAsync("Space");
        Assert.NotEmpty(filtered);
        Assert.All(filtered, c => Assert.True(
            c.Name.Contains("Space", StringComparison.OrdinalIgnoreCase) ||
            c.Code.Contains("Space", StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains("Space", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task EvaluateSymptomsAsync_MatchingSymptoms_ReturnsRankedMatchesWithDisclaimer()
    {
        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 1,
            SymptomCodes = new List<string> { "DIZZINESS", "FATIGUE" }
        };

        var result = await _service.EvaluateSymptomsAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Disclaimer));
        Assert.Contains("not a clinical diagnosis", result.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.ShouldEscalate);
        Assert.NotEmpty(result.Matches);
        Assert.True(result.Matches.First().MatchedSymptoms.Count >= result.Matches.Last().MatchedSymptoms.Count);
    }

    [Fact]
    public async Task EvaluateSymptomsAsync_UnknownCode_IsRejected()
    {
        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 1,
            SymptomCodes = new List<string> { "NON_EXISTENT_CODE" }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EvaluateSymptomsAsync(request));
    }

    [Fact]
    public async Task EvaluateSymptomsAsync_RedFlagSymptom_TriggersEmergencyAlertAndSetsEscalation()
    {
        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 1,
            SymptomCodes = new List<string> { "CHEST_PAIN", "FATIGUE" }
        };

        var result = await _service.EvaluateSymptomsAsync(request);

        Assert.NotNull(result);
        Assert.True(result.ShouldEscalate);
        Assert.NotNull(result.EscalationReason);
        Assert.Contains("Chest Pain", result.EscalationReason);
        Assert.Equal(999, result.TriggeredAlertId);

        Assert.Single(_alertService.TriggeredManualAlerts);
        Assert.Equal(1, _alertService.TriggeredManualAlerts[0].AstronautId);
        Assert.Equal(AlertSeverity.Emergency, _alertService.TriggeredManualAlerts[0].Severity);
        Assert.True(_alertService.TriggeredManualAlerts[0].ReuseOpenAssessmentAlert);
    }

    [Fact]
    public async Task EvaluateSymptomsAsync_DeactivatedAstronaut_ThrowsInvalidOperationException()
    {
        var astronaut = await _context.Astronauts.FirstAsync(a => a.Id == 1);
        astronaut.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 1,
            SymptomCodes = new List<string> { "FATIGUE" }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EvaluateSymptomsAsync(request));
        Assert.Contains("deactivated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateSymptomsAsync_SecondAstronaut_EscalatesForThatAstronaut()
    {
        _context.Astronauts.Add(new Astronaut
        {
            Id = 2, AstronautCode = "AST-02", FirstName = "Second", LastName = "Crew",
            FullName = "Second Crew", IsActive = true
        });
        await _context.SaveChangesAsync();
        var result = await _service.EvaluateSymptomsAsync(new EvaluateSymptomsRequest
        {
            AstronautId = 2, SymptomCodes = ["CHEST_PAIN"]
        });
        Assert.Equal(3, result.Level);
        Assert.Equal(2, Assert.Single(_alertService.TriggeredManualAlerts).AstronautId);
    }

    [Fact]
    public async Task EvaluateSymptomsAsync_NonExistentAstronaut_ThrowsKeyNotFoundException()
    {
        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 9999,
            SymptomCodes = new List<string> { "FATIGUE" }
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.EvaluateSymptomsAsync(request));
    }
    [Fact]
    public async Task IndividualSeverity_DoesNotSpreadToUnrelatedSymptomsOrPatterns()
    {
        var result=await _service.EvaluateSymptomsAsync(new(){AstronautId=1,SymptomCodes=["HEADACHE","FATIGUE"],SymptomDetails=[new(){Code="HEADACHE",Severity="severe",DurationHours=30},new(){Code="FATIGUE",Severity="mild"}]});
        Assert.Equal(2,result.SymptomFindings.Single(s=>s.Code=="HEADACHE").Level);
        Assert.Equal(1,result.SymptomFindings.Single(s=>s.Code=="FATIGUE").Level);
        Assert.Equal(1,result.ConditionMatches.Single(c=>c.ConditionCode=="SLEEP_DEFICIENCY").FollowUpLevel);
        Assert.Equal(2,result.ConditionMatches.Single(c=>c.ConditionCode=="SPACEFLIGHT_VISION_CHANGES").FollowUpLevel);
        Assert.Empty(_alertService.TriggeredManualAlerts);
    }
    [Fact]
    public async Task CriticalRelatedVital_AlertsDoctorForTargetAstronaut()
    {
        _context.VitalSignReadings.Add(new(){AstronautId=1,VitalSignTypeId=4,Value=40,Status=VitalSignStatus.Emergency,RecordedAt=DateTime.UtcNow});
        await _context.SaveChangesAsync();
        var result=await _service.EvaluateSymptomsAsync(new(){AstronautId=1,SymptomCodes=["CHILLS"],SymptomDetails=[new(){Code="CHILLS",Severity="mild"}]});
        Assert.Equal(3,result.Level);Assert.Equal(999,result.TriggeredAlertId);
        Assert.Equal(1,Assert.Single(_alertService.TriggeredManualAlerts).AstronautId);
        Assert.Equal(3,result.ConditionMatches.Single().FollowUpLevel);
    }
}
