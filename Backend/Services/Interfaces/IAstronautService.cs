using AstronautHealthMonitoring.DTOs.Astronaut;
using AstronautHealthMonitoring.DTOs.Common;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for managing astronaut demographic, flight assignment, and lifecycle profiles.
/// </summary>
public interface IAstronautService
{
    /// <summary>
    /// Retrieves paginated astronaut profiles with optional mission filtering.
    /// </summary>
    Task<PagedResult<AstronautDto>> GetAstronautsPagedAsync(int? missionId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all astronaut profiles for flight monitor oversight.
    /// </summary>
    Task<IReadOnlyList<AstronautDto>> GetAllAstronautsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an astronaut profile by its unique database identifier.
    /// </summary>
    Task<AstronautDto?> GetAstronautByIdAsync(int astronautId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an astronaut profile by operational astronaut code.
    /// </summary>
    Task<AstronautDto?> GetAstronautByCodeAsync(string astronautCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an astronaut profile linked to a specific Identity user ID.
    /// </summary>
    Task<AstronautDto?> GetAstronautByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically provisions an astronaut profile and associated Identity account in a single transaction.
    /// </summary>
    Task<AstronautDto> CreateAstronautAsync(CreateAstronautRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates profile fields of an existing astronaut. Server-side and identity fields cannot be modified.
    /// </summary>
    Task<AstronautDto> UpdateAstronautAsync(int astronautId, UpdateAstronautRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an astronaut: sets IsActive to false, locks their Identity account, and revokes all active refresh tokens.
    /// </summary>
    Task<bool> DeactivateAstronautAsync(int astronautId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns or reassigns an astronaut to an operational flight mission.
    /// </summary>
    Task<bool> AssignMissionAsync(int astronautId, int? missionId, CancellationToken cancellationToken = default);
}

