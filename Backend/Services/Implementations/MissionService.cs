using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Missions;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class MissionService : IMissionService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<MissionService> _logger;

    public MissionService(
        AppDbContext dbContext,
        IMapper mapper,
        ILogger<MissionService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MissionDto>> GetAllMissionsAsync(CancellationToken cancellationToken = default)
    {
        var missions = await _dbContext.Missions
            .Include(m => m.Astronauts)
            .AsNoTracking()
            .OrderBy(m => m.StartDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<MissionDto>>(missions);
    }

    public async Task<MissionDto?> GetMissionByIdAsync(int missionId, CancellationToken cancellationToken = default)
    {
        var mission = await _dbContext.Missions
            .Include(m => m.Astronauts)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);

        return mission != null ? _mapper.Map<MissionDto>(mission) : null;
    }

    public async Task<MissionDto?> GetMissionByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var mission = await _dbContext.Missions
            .Include(m => m.Astronauts)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == code, cancellationToken);

        return mission != null ? _mapper.Map<MissionDto>(mission) : null;
    }

    public async Task<MissionDto> CreateMissionAsync(CreateMissionRequest request, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Missions.AnyAsync(m => m.Code == request.Code, cancellationToken))
        {
            throw new InvalidOperationException($"Mission with code '{request.Code}' already exists.");
        }

        var mission = _mapper.Map<Mission>(request);
        mission.CreatedAt = DateTime.UtcNow;

        _dbContext.Missions.Add(mission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission '{Code} - {Name}' created successfully.", mission.Code, mission.Name);
        return _mapper.Map<MissionDto>(mission);
    }

    public async Task<MissionDto> UpdateMissionAsync(int missionId, UpdateMissionRequest request, CancellationToken cancellationToken = default)
    {
        var mission = await _dbContext.Missions
            .Include(m => m.Astronauts)
            .FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);

        if (mission == null)
        {
            throw new KeyNotFoundException($"Mission with ID {missionId} not found.");
        }

        _mapper.Map(request, mission);
        mission.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Mission '{Code}' updated successfully.", mission.Code);

        return _mapper.Map<MissionDto>(mission);
    }

    public async Task<bool> DeleteMissionAsync(int missionId, CancellationToken cancellationToken = default)
    {
        var mission = await _dbContext.Missions
            .Include(m => m.Astronauts)
            .FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);

        if (mission == null)
        {
            throw new KeyNotFoundException($"Mission with ID {missionId} not found.");
        }

        if (mission.Astronauts.Any())
        {
            throw new InvalidOperationException("Cannot delete mission because astronauts are currently assigned to it.");
        }

        _dbContext.Missions.Remove(mission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Mission with ID {MissionId} deleted successfully.", missionId);
        return true;
    }

    public async Task<bool> AssignAstronautAsync(int missionId, int astronautId, CancellationToken cancellationToken = default)
    {
        var missionExists = await _dbContext.Missions.AnyAsync(m => m.Id == missionId, cancellationToken);
        if (!missionExists)
        {
            throw new KeyNotFoundException($"Mission with ID {missionId} not found.");
        }

        var astronaut = await _dbContext.Astronauts.FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);
        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} not found.");
        }

        astronaut.MissionId = missionId;
        astronaut.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Astronaut {AstronautId} assigned to Mission {MissionId}.", astronautId, missionId);
        return true;
    }

    public async Task<bool> UnassignAstronautAsync(int missionId, int astronautId, CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts.FirstOrDefaultAsync(a => a.Id == astronautId && a.MissionId == missionId, cancellationToken);
        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} assigned to mission {missionId} not found.");
        }

        astronaut.MissionId = null;
        astronaut.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Astronaut {AstronautId} unassigned from Mission {MissionId}.", astronautId, missionId);
        return true;
    }
}

