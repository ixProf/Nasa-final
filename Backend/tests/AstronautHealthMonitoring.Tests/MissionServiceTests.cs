using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Missions;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Implementations;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class MissionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly MissionService _missionService;

    public MissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"MissionServiceTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);

        var config = new TypeAdapterConfig();
        new MissionMappings().Register(config);
        _mapper = new Mapper(config);

        _missionService = new MissionService(_context, _mapper, NullLogger<MissionService>.Instance);
    }

    [Fact]
    public async Task CreateMissionAsync_ValidRequest_CreatesMission()
    {
        var request = new CreateMissionRequest
        {
            Code = "ARTEMIS-V",
            Name = "Artemis V Lunar Base",
            Destination = "Moon - South Pole",
            StartDate = new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2027, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = MissionStatus.Scheduled
        };

        var result = await _missionService.CreateMissionAsync(request);

        Assert.NotNull(result);
        Assert.Equal("ARTEMIS-V", result.Code);
        Assert.Equal("Artemis V Lunar Base", result.Name);
        Assert.Equal(MissionStatus.Scheduled, result.Status);
        Assert.Equal(0, result.AstronautsCount);

        var inDb = await _context.Missions.FirstOrDefaultAsync(m => m.Code == "ARTEMIS-V");
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task CreateMissionAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        var request = new CreateMissionRequest
        {
            Code = "DUP-CODE",
            Name = "Original Mission",
            Destination = "LEO",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Active
        };
        await _missionService.CreateMissionAsync(request);

        var duplicateRequest = new CreateMissionRequest
        {
            Code = "DUP-CODE",
            Name = "Duplicate Mission",
            Destination = "Moon",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Scheduled
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _missionService.CreateMissionAsync(duplicateRequest));
        Assert.Contains("Mission with code 'DUP-CODE' already exists", ex.Message);
    }

    [Fact]
    public async Task GetAllMissionsAsync_ReturnsAllMissionsWithAstronautCount()
    {
        var mission1 = new Mission
        {
            Code = "MSN-A",
            Name = "Mission A",
            Destination = "Mars",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Active
        };
        var mission2 = new Mission
        {
            Code = "MSN-B",
            Name = "Mission B",
            Destination = "Venus",
            StartDate = DateTime.UtcNow.AddMonths(1),
            Status = MissionStatus.Scheduled
        };
        _context.Missions.AddRange(mission1, mission2);
        await _context.SaveChangesAsync();

        var astronaut = new Astronaut
        {
            AstronautCode = "AST-M1",
            FirstName = "Test",
            LastName = "Astro",
            FullName = "Test Astro",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "O+",
            HeightCm = 178,
            WeightKg = 72,
            MissionId = mission1.Id
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var result = await _missionService.GetAllMissionsAsync();

        Assert.Equal(2, result.Count);
        var foundMission1 = result.First(m => m.Code == "MSN-A");
        Assert.Equal(1, foundMission1.AstronautsCount);

        var foundMission2 = result.First(m => m.Code == "MSN-B");
        Assert.Equal(0, foundMission2.AstronautsCount);
    }

    [Fact]
    public async Task DeleteMissionAsync_WithNoAstronauts_DeletesSuccessfully()
    {
        var mission = new Mission
        {
            Code = "EMPTY-MSN",
            Name = "Empty Mission",
            Destination = "Mars",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Completed
        };
        _context.Missions.Add(mission);
        await _context.SaveChangesAsync();

        var success = await _missionService.DeleteMissionAsync(mission.Id);
        Assert.True(success);

        var inDb = await _context.Missions.FindAsync(mission.Id);
        Assert.Null(inDb);
    }

    [Fact]
    public async Task DeleteMissionAsync_WithAssignedAstronauts_ThrowsInvalidOperationException()
    {
        var mission = new Mission
        {
            Code = "BUSY-MSN",
            Name = "Busy Mission",
            Destination = "Mars",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Active
        };
        _context.Missions.Add(mission);
        await _context.SaveChangesAsync();

        var astronaut = new Astronaut
        {
            AstronautCode = "AST-BUSY",
            FirstName = "Busy",
            LastName = "Astro",
            FullName = "Busy Astro",
            DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Female",
            BloodType = "A+",
            HeightCm = 165,
            WeightKg = 58,
            MissionId = mission.Id
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _missionService.DeleteMissionAsync(mission.Id));
        Assert.Contains("Cannot delete mission because astronauts are currently assigned to it", ex.Message);
    }

    [Fact]
    public async Task AssignAndUnassignAstronaut_UpdatesAstronautMissionId()
    {
        var mission = new Mission
        {
            Code = "ASSIGN-MSN",
            Name = "Assign Mission",
            Destination = "LEO",
            StartDate = DateTime.UtcNow,
            Status = MissionStatus.Scheduled
        };
        _context.Missions.Add(mission);
        await _context.SaveChangesAsync();

        var astronaut = new Astronaut
        {
            AstronautCode = "AST-ASSIGN",
            FirstName = "Assignee",
            LastName = "Astro",
            FullName = "Assignee Astro",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Male",
            BloodType = "B+",
            HeightCm = 180,
            WeightKg = 75,
            MissionId = null
        };
        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        // Assign
        var assignSuccess = await _missionService.AssignAstronautAsync(mission.Id, astronaut.Id);
        Assert.True(assignSuccess);

        var astronautAfterAssign = await _context.Astronauts.FindAsync(astronaut.Id);
        Assert.Equal(mission.Id, astronautAfterAssign!.MissionId);

        // Unassign
        var unassignSuccess = await _missionService.UnassignAstronautAsync(mission.Id, astronaut.Id);
        Assert.True(unassignSuccess);

        var astronautAfterUnassign = await _context.Astronauts.FindAsync(astronaut.Id);
        Assert.Null(astronautAfterUnassign!.MissionId);
    }

    [Fact]
    public void Mission_ForeignKeys_DoNotUseCascadeDelete()
    {
        var missionType = _context.Model.FindEntityType(typeof(Mission));
        Assert.NotNull(missionType);

        var foreignKeys = _context.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.PrincipalEntityType == missionType)
            .ToList();

        Assert.NotEmpty(foreignKeys);

        foreach (var fk in foreignKeys)
        {
            Assert.NotEqual(DeleteBehavior.Cascade, fk.DeleteBehavior);
            Assert.NotEqual(DeleteBehavior.ClientCascade, fk.DeleteBehavior);
        }
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

