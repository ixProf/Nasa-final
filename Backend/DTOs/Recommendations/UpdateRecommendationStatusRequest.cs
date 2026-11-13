using AstronautHealthMonitoring.Models.Enums;

namespace AstronautHealthMonitoring.DTOs.Recommendations;

public class UpdateRecommendationStatusRequest
{
    public RecommendationStatus Status { get; set; }
}
