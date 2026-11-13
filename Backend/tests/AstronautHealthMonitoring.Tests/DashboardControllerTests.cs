using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Dashboard;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class DashboardControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly DashboardService _dashboardService;
    private readonly TestCurrentUserService _currentUserService;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"DashboardControllerTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        var config = new TypeAdapterConfig();
        new AstronautMappings().Register(config);
        new VitalSignMappings().Register(config);
        new AlertMappings().Register(config);
        new SleepMappings().Register(config);
        new MoodMappings().Register(config);
        new RecommendationMappings().Register(config);
        new MissionMappings().Register(config);
        _mapper = new Mapper(config);

        _dashboardService = new DashboardService(_context, _mapper, NullLogger<DashboardService>.Instance);

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Monitor,
            UserId = "monitor-01",
            AstronautId = null,
            FullName = "Flight Director",
            IsAuthenticated = true
        };

        _controller = new DashboardController(_dashboardService, _currentUserService);
    }

    [Fact]
    public async Task GetMonitorDashboard_WhenMonitor_Returns200OK()
    {
        var result = await _controller.GetMonitorDashboard(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<MonitorDashboardSummaryDto>(okResult.Value);
        Assert.NotNull(dto);
    }

    [Fact]
    public async Task GetAstronautSnapshot_WhenAstronautExists_Returns200OK()
    {
        var astronaut = new Astronaut
        {
            AstronautCode = "AST-TEST-SNAP",
            FirstName = "Victor",
            LastName = "Glover",
            FullName = "Victor Glover",
            DateOfBirth = new DateTime(1976, 4, 30, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 178,
            WeightKg = 73,
            IsActive = true
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var result = await _controller.GetAstronautSnapshot(astronaut.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AstronautDashboardSummaryDto>(okResult.Value);
        Assert.Equal("Victor Glover", dto.FullName);
    }

    [Fact]
    public async Task GetAstronautSnapshot_WhenNotFound_Returns404NotFound()
    {
        var result = await _controller.GetAstronautSnapshot(999999, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetMyDashboard_WhenAuthenticatedAstronaut_Returns200OK()
    {
        var astronaut = new Astronaut
        {
            AstronautCode = "AST-MY-DASH",
            FirstName = "Jessica",
            LastName = "Watkins",
            FullName = "Jessica Watkins",
            DateOfBirth = new DateTime(1988, 5, 14, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 170,
            WeightKg = 61,
            IsActive = true
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astronaut.Id;
        _currentUserService.UserId = "user-jessica";

        var result = await _controller.GetMyDashboard(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AstronautDashboardSummaryDto>(okResult.Value);
        Assert.Equal("Jessica Watkins", dto.FullName);
        Assert.Equal(astronaut.Id, dto.AstronautId);
    }

    [Fact]
    public async Task GetMyDashboard_WhenAstronautClaimMissing_Returns403Forbidden()
    {
        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = null;

        var result = await _controller.GetMyDashboard(CancellationToken.None);

        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
