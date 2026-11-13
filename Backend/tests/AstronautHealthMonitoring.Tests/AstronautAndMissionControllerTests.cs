using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Astronaut;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Missions;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Validators.Astronaut;
using AstronautHealthMonitoring.Validators.Mission;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class AstronautAndMissionControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly AstronautService _astronautService;
    private readonly MissionService _missionService;
    private readonly TestCurrentUserService _currentUserService;
    private readonly AstronautsController _astronautsController;
    private readonly MissionsController _missionsController;

    public AstronautAndMissionControllerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"ControllerTests_{Guid.NewGuid()}"));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<AppDbContext>();
        _userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        _roleManager.CreateAsync(new IdentityRole(UserRoles.Astronaut)).GetAwaiter().GetResult();
        _roleManager.CreateAsync(new IdentityRole(UserRoles.Monitor)).GetAwaiter().GetResult();

        var config = new TypeAdapterConfig();
        new AstronautMappings().Register(config);
        new MissionMappings().Register(config);
        _mapper = new Mapper(config);

        _astronautService = new AstronautService(
            _context,
            _userManager,
            _mapper,
            NullLogger<AstronautService>.Instance);

        _missionService = new MissionService(
            _context,
            _mapper,
            NullLogger<MissionService>.Instance);

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Monitor,
            UserId = "monitor-01",
            AstronautId = null,
            FullName = "Flight Director",
            IsAuthenticated = true
        };

        _astronautsController = new AstronautsController(
            _astronautService,
            _currentUserService,
            new CreateAstronautRequestValidator(),
            new UpdateAstronautRequestValidator());

        _missionsController = new MissionsController(
            _missionService,
            _astronautService,
            _currentUserService,
            new CreateMissionRequestValidator(),
            new UpdateMissionRequestValidator());
    }

    [Fact]
    public async Task Astronaut_Cannot_View_Another_Astronaut_Profile()
    {
        // Setup two astronauts
        var astro1 = await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "astro1",
            Email = "astro1@nasa.gov",
            InitialPassword = "Password123!",
            AstronautCode = "AST-001",
            FirstName = "Neil",
            LastName = "Armstrong",
            DateOfBirth = new DateTime(1930, 8, 5, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 180,
            WeightKg = 77
        });

        var astro2 = await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "astro2",
            Email = "astro2@nasa.gov",
            InitialPassword = "Password123!",
            AstronautCode = "AST-002",
            FirstName = "Buzz",
            LastName = "Aldrin",
            DateOfBirth = new DateTime(1930, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "A+",
            HeightCm = 178,
            WeightKg = 75
        });

        // Act as astro1
        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astro1.Id;
        _currentUserService.UserId = astro1.UserId;

        // Try to access astro2's profile
        var result = await _astronautsController.GetById(astro2.Id, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public async Task Astronaut_Can_View_Own_Profile_And_Me()
    {
        var astro = await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "astro.self",
            Email = "self@nasa.gov",
            InitialPassword = "Password123!",
            AstronautCode = "AST-SELF",
            FirstName = "Sally",
            LastName = "Ride",
            DateOfBirth = new DateTime(1951, 5, 26, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "B+",
            HeightCm = 165,
            WeightKg = 57
        });

        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astro.Id;
        _currentUserService.UserId = astro.UserId;

        // GetById
        var resultById = await _astronautsController.GetById(astro.Id, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(resultById);
        var dto = Assert.IsType<AstronautDto>(okResult.Value);
        Assert.Equal("Sally Ride", dto.FullName);

        // GetMe
        var resultMe = await _astronautsController.GetMe(CancellationToken.None);
        var okMe = Assert.IsType<OkObjectResult>(resultMe);
        var dtoMe = Assert.IsType<AstronautDto>(okMe.Value);
        Assert.Equal("Sally Ride", dtoMe.FullName);
    }

    [Fact]
    public async Task Monitor_Can_View_Any_Astronaut()
    {
        var astro = await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "astro.viewed",
            Email = "viewed@nasa.gov",
            InitialPassword = "Password123!",
            AstronautCode = "AST-VIEW",
            FirstName = "Michael",
            LastName = "Collins",
            DateOfBirth = new DateTime(1930, 10, 31, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "AB+",
            HeightCm = 180,
            WeightKg = 75
        });

        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var result = await _astronautsController.GetById(astro.Id, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AstronautDto>(okResult.Value);
        Assert.Equal("Michael Collins", dto.FullName);
    }

    [Fact]
    public async Task Astronaut_Can_Only_View_Their_Assigned_Mission()
    {
        var mission1 = await _missionService.CreateMissionAsync(new CreateMissionRequest
        {
            Code = "APOLLO-11",
            Name = "Apollo 11",
            Destination = "Moon",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Active
        });

        var mission2 = await _missionService.CreateMissionAsync(new CreateMissionRequest
        {
            Code = "APOLLO-12",
            Name = "Apollo 12",
            Destination = "Moon",
            StartDate = DateTime.UtcNow.AddMonths(4),
            Status = MissionStatus.Scheduled
        });

        var astro = await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "astro.mission",
            Email = "mission@nasa.gov",
            InitialPassword = "Password123!",
            AstronautCode = "AST-MSN-01",
            FirstName = "Pete",
            LastName = "Conrad",
            DateOfBirth = new DateTime(1930, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "A+",
            HeightCm = 170,
            WeightKg = 68,
            MissionId = mission2.Id
        });

        _currentUserService.Role = UserRoles.Astronaut;
        _currentUserService.AstronautId = astro.Id;
        _currentUserService.UserId = astro.UserId;

        // GET /api/missions should only return mission2
        var missionsResult = await _missionsController.GetMissions(CancellationToken.None);
        var okMissions = Assert.IsType<OkObjectResult>(missionsResult);
        var missionList = Assert.IsAssignableFrom<IEnumerable<MissionDto>>(okMissions.Value);
        var list = missionList.ToList();
        Assert.Single(list);
        Assert.Equal("APOLLO-12", list[0].Code);

        // GET /api/missions/{mission1.Id} should be forbidden
        var forbiddenResult = await _missionsController.GetById(mission1.Id, CancellationToken.None);
        var statusForbidden = Assert.IsType<ObjectResult>(forbiddenResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusForbidden.StatusCode);

        // GET /api/missions/{mission2.Id} should succeed
        var allowedResult = await _missionsController.GetById(mission2.Id, CancellationToken.None);
        var okAllowed = Assert.IsType<OkObjectResult>(allowedResult);
        var singleMission = Assert.IsType<MissionDto>(okAllowed.Value);
        Assert.Equal("APOLLO-12", singleMission.Code);
    }

    [Fact]
    public async Task DeleteMission_WithAssignedAstronauts_Returns409Conflict()
    {
        var mission = await _missionService.CreateMissionAsync(new CreateMissionRequest
        {
            Code = "CONFLICT-MSN",
            Name = "Conflict Mission",
            Destination = "LEO",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Active
        });

        await _astronautService.CreateAstronautAsync(new CreateAstronautRequest
        {
            Username = "astro.assigned",
            Email = "assigned@nasa.gov",
            InitialPassword = "Password123!",
            AstronautCode = "AST-CONFLICT",
            FirstName = "Assigned",
            LastName = "Astronaut",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 180,
            WeightKg = 75,
            MissionId = mission.Id
        });

        _currentUserService.Role = UserRoles.Monitor;

        var deleteResult = await _missionsController.DeleteMission(mission.Id, CancellationToken.None);
        var conflictResult = Assert.IsType<ConflictObjectResult>(deleteResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

