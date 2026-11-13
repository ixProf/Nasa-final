using AstronautHealthMonitoring.Models.Common;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Structured health assessment report summarizing telemetry, trends, and clinical readiness.
/// </summary>
public class HealthReport : BaseEntity
{
    public int AstronautId { get; set; }
    public Astronaut Astronaut { get; set; } = null!;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public ReportType ReportType { get; set; } = ReportType.DailySummary;

    /// <summary>
    /// Composite wellness score calculated from vitals, sleep, mood, and exercise (0 - 100).
    /// </summary>
    public double OverallHealthScore { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string KeyObservations { get; set; } = string.Empty;
    public string RecommendationsSummary { get; set; } = string.Empty;

    /// <summary>
    /// Identifier or name of generator (e.g. "Automated Health Engine" or "Ground Monitor Lead").
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
}

