namespace AstronautHealthMonitoring.DTOs.Mood;

public class MoodEntryDto
{
    public int Id { get; set; }
    public Guid ClientGeneratedId { get; set; }
    public int AstronautId { get; set; }
    public string AstronautFullName { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; }
    public int MoodScore { get; set; }
    public int MoodScaleMaximum { get; set; } = 10;
    public int StressLevel { get; set; }
    public string DominantEmotion { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
