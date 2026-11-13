using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Dashboard;

public class AstronautDashboardSummaryDto
{
    public int AstronautId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AstronautCode { get; set; } = string.Empty;
    public string? MissionName { get; set; }
    public bool IsActive { get; set; } = true;

    public VitalSignStatus OverallStatus { get; set; } = VitalSignStatus.Routine;
    public DateTime? LastSyncAt { get; set; }

    public List<VitalSignReadingDto> RecentVitals { get; set; } = new();
    public List<HealthAlertDto> ActiveAlerts { get; set; } = new();
    public SleepRecordDto? LatestSleep { get; set; }
    public MoodEntryDto? LatestMood { get; set; }
    public List<RecommendationDto> PendingRecommendations { get; set; } = new();
    public int UnreadChatMessagesCount { get; set; }
}

