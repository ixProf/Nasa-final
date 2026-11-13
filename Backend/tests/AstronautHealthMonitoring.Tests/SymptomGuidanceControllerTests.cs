using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Validators.SymptomGuidance;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class SymptomGuidanceControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly TestHealthAlertService _alertService;
    private readonly SymptomGuidanceService _service;
    private readonly TestCurrentUserService _currentUserService;
    private readonly SymptomGuidanceController _controller;

    public SymptomGuidanceControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"SymptomGuidanceControllerDb_{Guid.NewGuid()}")
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

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Astronaut,
            UserId = "astro-01",
            AstronautId = 1,
            FullName = "Major Tom",
            IsAuthenticated = true
        };

        _controller = new SymptomGuidanceController(
            _service,
            _currentUserService,
            new EvaluateSymptomsRequestValidator());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AssessSymptoms_AstronautAssessingForSelf_Succeeds()
    {
        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 1,
            SymptomCodes = new List<string> { "DIZZINESS" }
        };

        var result = await _controller.AssessSymptoms(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SymptomAssessmentResponseDto>(okResult.Value);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task AssessSymptoms_AstronautAssessingForOther_ReturnsForbidden()
    {
        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 2,
            SymptomCodes = new List<string> { "DIZZINESS" }
        };

        var result = await _controller.AssessSymptoms(request);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task AssessSymptoms_MonitorSpecifyingAstronaut_Succeeds()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 1,
            SymptomCodes = new List<string> { "DIZZINESS" }
        };

        var result = await _controller.AssessSymptoms(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SymptomAssessmentResponseDto>(okResult.Value);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task AssessSymptoms_MonitorMissingAstronautId_ReturnsBadRequest()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 0,
            SymptomCodes = new List<string> { "DIZZINESS" }
        };

        var result = await _controller.AssessSymptoms(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task AssessSymptoms_ValidationFailure_ReturnsValidationProblem()
    {
        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 1,
            SymptomCodes = new List<string>() // Empty symptoms
        };

        var result = await _controller.AssessSymptoms(request);

        var badRequest = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }

    [Fact]
    public async Task AssessSymptoms_DeactivatedAstronaut_ReturnsConflict()
    {
        var astronaut = await _context.Astronauts.FirstAsync(a => a.Id == 1);
        astronaut.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new EvaluateSymptomsRequest
        {
            AstronautId = 1,
            SymptomCodes = new List<string> { "DIZZINESS" }
        };

        var result = await _controller.AssessSymptoms(request);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task GetAllSymptoms_ReturnsOkWithCatalog()
    {
        var result = await _controller.GetAllSymptoms();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var symptoms = Assert.IsAssignableFrom<IReadOnlyList<SymptomDto>>(okResult.Value);
        Assert.NotEmpty(symptoms);
    }

    [Fact]
    public async Task GetGuidanceCatalog_ReturnsOkWithConditions()
    {
        var result = await _controller.GetGuidanceCatalog();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var conditions = Assert.IsAssignableFrom<IReadOnlyList<SpaceConditionDto>>(okResult.Value);
        Assert.NotEmpty(conditions);
    }
}
