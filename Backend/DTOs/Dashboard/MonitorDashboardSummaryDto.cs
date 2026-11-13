using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Dashboard;

public class MonitorDashboardSummaryDto
{
    public int TotalMonitoredAstronauts { get; set; }
    public int ActiveAstronautsCount { get; set; }
    public int DeactivatedAstronautsCount { get; set; }
    public int ActiveMissionsCount { get; set; }
    public int ActiveAlertsCount { get; set; }
    public int EmergencyAlertsCount { get; set; }
    public int MedicalAttentionAlertsCount { get; set; }

    public List<MissionSummaryDto> MissionSummaries { get; set; } = new();
    public List<HealthAlertDto> CriticalAlerts { get; set; } = new();
    public List<AstronautStatusSummaryDto> CrewSummaries { get; set; } = new();
}

public class AstronautStatusSummaryDto
{
    public int AstronautId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AstronautCode { get; set; } = string.Empty;
    public string? MissionName { get; set; }
    public bool IsActive { get; set; } = true;
    public VitalSignStatus OverallStatus { get; set; } = VitalSignStatus.Routine;
    public int ActiveAlertsCount { get; set; }
    public int EmergencyAlertsCount { get; set; }
    public int MedicalAttentionAlertsCount { get; set; }
    public DateTime? LastReadingRecordedAt { get; set; }
    public List<VitalSignReadingDto> LatestVitals { get; set; } = new();
}

public class MissionSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public MissionStatus Status { get; set; }
    public int CrewCount { get; set; }
}
