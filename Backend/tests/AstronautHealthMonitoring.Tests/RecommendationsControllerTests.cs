using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Validators.Recommendations;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class RecommendationsControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly RecommendationService _service;
    private readonly TestCurrentUserService _currentUserService;
    private readonly RecommendationsController _controller;

    public RecommendationsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"RecommendationsControllerDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        var config = new TypeAdapterConfig();
        new RecommendationMappings().Register(config);
        _mapper = new Mapper(config);

        _service = new RecommendationService(_context, _mapper, NullLogger<RecommendationService>.Instance);

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Monitor,
            UserId = "monitor-01",
            AstronautId = null,
            FullName = "Flight Surgeon",
            IsAuthenticated = true
        };

        _controller = new RecommendationsController(
            _service,
            _currentUserService,
            new CreateRecommendationRequestValidator(),
            new UpdateRecommendationStatusRequestValidator());
    }

    [Fact]
    public async Task Astronaut_Cannot_View_Another_Astronauts_Recommendations()
    {
        var astro1 = new Astronaut
        {
            AstronautCode = "AST-CTRL-A1",
            FirstName = "Astro",
            LastName = "One",
            FullName = "Astro One",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 175,
            WeightKg = 70,
            IsActive = true
        };
        var astro2 = new Astronaut
        {
            AstronautCode = "AST-CTRL-A2",
            FirstName = "Astro",
            LastName = "Two",
            FullName = "Astro Two",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 168,
            WeightKg = 60,
            IsActive = true
        };
        _context.Astronauts.AddRange(astro1, astro2);
        await _context.SaveChangesAsync();

        // Act as astro1
        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astro1.Id;

        // Try to query astro2's recommendations
        var result = await _controller.GetRecommendations(astronautId: astro2.Id, pendingOnly: true, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Astronaut_Can_View_Own_Recommendations()
    {
        var astro = new Astronaut
        {
            AstronautCode = "AST-CTRL-OWN",
            FirstName = "Astro",
            LastName = "Own",
            FullName = "Astro Own",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 175,
            WeightKg = 70,
            IsActive = true
        };
        _context.Astronauts.Add(astro);
        await _context.SaveChangesAsync();

        _context.Recommendations.Add(new Recommendation
        {
            AstronautId = astro.Id,
            Title = "Exercise Session",
            Content = "Cycle ergometer 45 mins",
            Reason = "Cardiovascular fitness",
            Status = RecommendationStatus.Pending
        });
        await _context.SaveChangesAsync();

        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astro.Id;

        var result = await _controller.GetRecommendations(astronautId: null, pendingOnly: true, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var recs = Assert.IsAssignableFrom<IEnumerable<RecommendationDto>>(okResult.Value);
        Assert.Single(recs);
    }

    [Fact]
    public async Task Astronaut_Cannot_View_Another_Astronauts_Recommendation_ById()
    {
        var astro1 = new Astronaut
        {
            AstronautCode = "AST-CTRL-ID1",
            FirstName = "Astro",
            LastName = "One",
            FullName = "Astro One",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 175,
            WeightKg = 70,
            IsActive = true
        };
        var astro2 = new Astronaut
        {
            AstronautCode = "AST-CTRL-ID2",
            FirstName = "Astro",
            LastName = "Two",
            FullName = "Astro Two",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 168,
            WeightKg = 60,
            IsActive = true
        };
        _context.Astronauts.AddRange(astro1, astro2);
        await _context.SaveChangesAsync();

        var rec = new Recommendation
        {
            AstronautId = astro2.Id,
            Title = "Astro2 Recommendation",
            Content = "Content",
            Reason = "Reason",
            Status = RecommendationStatus.Pending
        };
        _context.Recommendations.Add(rec);
        await _context.SaveChangesAsync();

        // Act as astro1
        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astro1.Id;

        var result = await _controller.GetById(rec.Id, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Monitor_Can_Create_Recommendation()
    {
        var astro = new Astronaut
        {
            AstronautCode = "AST-CREATE-REC",
            FirstName = "Flight",
            LastName = "Commander",
            FullName = "Flight Commander",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 175,
            WeightKg = 70,
            IsActive = true
        };
        _context.Astronauts.Add(astro);
        await _context.SaveChangesAsync();

        _currentUserService.Role = UserRoles.Monitor;

        var request = new CreateRecommendationRequest
        {
            AstronautId = astro.Id,
            Title = "CO2 Scrubbing Protocol",
            Content = "Switch scrubbers to secondary bed.",
            Reason = "Slight elevated environmental CO2 reading.",
            Category = RecommendationCategory.Environmental,
            Priority = RecommendationPriority.High
        };

        var result = await _controller.CreateRecommendation(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        var dto = Assert.IsType<RecommendationDto>(createdResult.Value);
        Assert.Equal("CO2 Scrubbing Protocol", dto.Title);
    }

    [Fact]
    public async Task Astronaut_Can_Update_Status_Of_Own_Recommendation()
    {
        var astro = new Astronaut
        {
            AstronautCode = "AST-UPDATE-STATUS",
            FirstName = "Astro",
            LastName = "Updater",
            FullName = "Astro Updater",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 168,
            WeightKg = 60,
            IsActive = true
        };
        _context.Astronauts.Add(astro);
        await _context.SaveChangesAsync();

        var rec = new Recommendation
        {
            AstronautId = astro.Id,
            Title = "Mental Health Check-in",
            Content = "Complete psychological log.",
            Reason = "Routine mission support.",
            Status = RecommendationStatus.Pending
        };
        _context.Recommendations.Add(rec);
        await _context.SaveChangesAsync();

        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astro.Id;

        var updateRequest = new UpdateRecommendationStatusRequest
        {
            Status = RecommendationStatus.Acknowledged
        };

        var result = await _controller.UpdateStatus(rec.Id, updateRequest, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<RecommendationDto>(okResult.Value);
        Assert.Equal(RecommendationStatus.Acknowledged, dto.Status);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_Returns409Conflict()
    {
        var astro = new Astronaut
        {
            AstronautCode = "AST-CONFLICT-TR",
            FirstName = "Conflict",
            LastName = "Test",
            FullName = "Conflict Test",
            DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 175,
            WeightKg = 70,
            IsActive = true
        };
        _context.Astronauts.Add(astro);
        await _context.SaveChangesAsync();

        var rec = new Recommendation
        {
            AstronautId = astro.Id,
            Title = "Terminal Rec",
            Content = "Content",
            Reason = "Reason",
            Status = RecommendationStatus.Completed // Terminal state
        };
        _context.Recommendations.Add(rec);
        await _context.SaveChangesAsync();

        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astro.Id;

        var request = new UpdateRecommendationStatusRequest { Status = RecommendationStatus.Pending };
        var result = await _controller.UpdateStatus(rec.Id, request, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

