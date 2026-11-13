namespace AstronautHealthMonitoring.Models.Entities;

public class HabitCompletion
{
    public int Id { get; set; }
    public int RecommendationId { get; set; }
    public Recommendation Recommendation { get; set; } = null!;
    public int AstronautId { get; set; }
    public DateOnly CompletedOn { get; set; }
    public DateTime CompletedAt { get; set; }
    public int Points { get; set; }
}
