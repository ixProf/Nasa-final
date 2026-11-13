using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Earth-to-Space communications link telemetry (latency and blackout windows affecting telemedicine support).
/// </summary>
public class CommunicationStatus : BaseEntity
{
    public int MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// One-way signal propagation delay in seconds (e.g. Moon ~1.3s, Mars ~180-1200s).
    /// </summary>
    public double LatencySeconds { get; set; }

    /// <summary>
    /// Signal strength percentage (0-100%).
    /// </summary>
    public double SignalStrengthPercentage { get; set; }

    public ConnectionState State { get; set; } = ConnectionState.Connected;

    /// <summary>
    /// Predicted timestamp when the next high-bandwidth communication window opens.
    /// </summary>
    public DateTime? NextWindowStart { get; set; }

    /// <summary>
    /// Predicted timestamp when current communication window closes.
    /// </summary>
    public DateTime? NextWindowEnd { get; set; }
}

