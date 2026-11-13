using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.Sleep;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for recording and reviewing circadian stability and sleep telemetry.
/// </summary>
public interface ISleepService
{
    /// <summary>
    /// Logs a sleep session for an astronaut; idempotent on ClientGeneratedId to prevent duplicate ingestion.
    /// Calculates duration and verifies efficiency boundaries.
    /// </summary>
    Task<SleepRecordDto> RecordSleepAsync(int astronautId, CreateSleepRecordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single sleep record by its ID, scoped by caller authorization.
    /// </summary>
    Task<SleepRecordDto?> GetSleepRecordByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated sleep session history with optional date range filters.
    /// </summary>
    Task<PagedResult<SleepRecordDto>> GetSleepHistoryPagedAsync(int astronautId, DateTime? fromUtc, DateTime? toUtc, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves sleep records for an astronaut within a specified date window.
    /// </summary>
    Task<IReadOnlyList<SleepRecordDto>> GetSleepHistoryAsync(int astronautId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent sleep session recorded for an astronaut.
    /// </summary>
    Task<SleepRecordDto?> GetLatestSleepRecordAsync(int astronautId, CancellationToken cancellationToken = default);
}
