using AstronautHealthMonitoring.DTOs.Sync;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for processing offline synchronization batches of health telemetry and messages.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Synchronizes a batch of telemetry and chat messages for an astronaut with idempotent replay protection.
    /// </summary>
    Task<SyncBatchResponse> ProcessSyncBatchAsync(
        int astronautId,
        SyncBatchRequest request,
        string userId,
        CancellationToken cancellationToken = default);
}

