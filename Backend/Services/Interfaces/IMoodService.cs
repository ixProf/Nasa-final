using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.Mood;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for recording and reviewing astronaut psychological check-ins and stress levels.
/// </summary>
public interface IMoodService
{
    /// <summary>
    /// Submits a psychological check-in entry for an astronaut; idempotent on ClientGeneratedId.
    /// </summary>
    Task<MoodEntryDto> RecordMoodAsync(int astronautId, CreateMoodEntryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single mood entry by its ID, scoped by caller authorization.
    /// </summary>
    Task<MoodEntryDto?> GetMoodEntryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated mood check-in history with optional date range filters.
    /// </summary>
    Task<PagedResult<MoodEntryDto>> GetMoodHistoryPagedAsync(int astronautId, DateTime? fromUtc, DateTime? toUtc, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves psychological check-in history for an astronaut within a time range.
    /// </summary>
    Task<IReadOnlyList<MoodEntryDto>> GetMoodHistoryAsync(int astronautId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the latest psychological check-in entry for an astronaut.
    /// </summary>
    Task<MoodEntryDto?> GetLatestMoodEntryAsync(int astronautId, CancellationToken cancellationToken = default);
}
