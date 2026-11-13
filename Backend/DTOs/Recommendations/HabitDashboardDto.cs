namespace AstronautHealthMonitoring.DTOs.Recommendations;

public record HabitDayDto(DateOnly Date, int Count, int Points);
public record HabitAssignmentDto(int Id, string Title, string? TitleAr, string Content, string? ContentAr,
    string Reason, string? ReasonAr, int Points, bool CompletedToday);
public class HabitDashboardDto
{
    public List<HabitAssignmentDto> Assignments { get; set; } = [];
    public List<HabitDayDto> Heatmap { get; set; } = [];
    public int TotalPoints { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalCompletions { get; set; }
    public int Level => 1 + TotalPoints / 100;
    public string[] Badges { get; set; } = [];
    public string TimeZone => "UTC";
}
