using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.VitalSigns;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for recording, batch syncing, and querying physiological telemetry.
/// </summary>
public interface IVitalSignService
{
    /// <summary>
    /// Records a vital sign reading for an astronaut; idempotent on ClientGeneratedId to ignore offline sync duplicates.
    /// Evaluates reading against active reference ranges and triggers health alerts when non-routine.
    /// </summary>
    Task<VitalSignReadingDto> RecordReadingAsync(int astronautId, CreateVitalSignReadingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single vital sign reading by its identifier, scoped to the caller's authorization.
    /// </summary>
    Task<VitalSignReadingDto?> GetReadingByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the single most recent vital sign reading for each distinct metric/vital sign type for an astronaut.
    /// </summary>
    Task<IReadOnlyList<VitalSignReadingDto>> GetLatestReadingsPerMetricAsync(int astronautId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves historical vital sign readings with optional UTC time window filtering and pagination.
    /// </summary>
    Task<PagedResult<VitalSignReadingDto>> GetHistoryAsync(int astronautId, DateTime? fromUtc, DateTime? toUtc, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent vital sign readings for an astronaut.
    /// </summary>
    Task<IReadOnlyList<VitalSignReadingDto>> GetRecentReadingsAsync(int astronautId, int count = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves vital sign readings for an astronaut within a specified device capture time window.
    /// </summary>
    Task<IReadOnlyList<VitalSignReadingDto>> GetReadingsByTimeRangeAsync(int astronautId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingests a batch of offline vital sign readings for an astronaut; idempotent on ClientGeneratedId for each reading.
    /// </summary>
    Task<IReadOnlyList<VitalSignReadingDto>> SyncReadingsAsync(int astronautId, SyncVitalSignReadingsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active monitored vital sign types and metric units.
    /// </summary>
    Task<IReadOnlyList<VitalSignTypeDto>> GetAllVitalSignTypesAsync(CancellationToken cancellationToken = default);
}
