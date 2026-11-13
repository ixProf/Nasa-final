using AstronautHealthMonitoring.DTOs.Missions;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for managing operational flight exploration missions.
/// </summary>
public interface IMissionService
{
    /// <summary>
    /// Retrieves all operational flight missions.
    /// </summary>
    Task<IReadOnlyList<MissionDto>> GetAllMissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a mission by its unique database identifier.
    /// </summary>
    Task<MissionDto?> GetMissionByIdAsync(int missionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a mission by its operational code.
    /// </summary>
    Task<MissionDto?> GetMissionByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new operational flight mission.
    /// </summary>
    Task<MissionDto> CreateMissionAsync(CreateMissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an operational flight mission.
    /// </summary>
    Task<MissionDto> UpdateMissionAsync(int missionId, UpdateMissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a mission if no astronauts are currently assigned to it.
    /// </summary>
    Task<bool> DeleteMissionAsync(int missionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns an astronaut to a mission.
    /// </summary>
    Task<bool> AssignAstronautAsync(int missionId, int astronautId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unassigns an astronaut from a mission.
    /// </summary>
    Task<bool> UnassignAstronautAsync(int missionId, int astronautId, CancellationToken cancellationToken = default);
}

