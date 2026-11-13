using AstronautHealthMonitoring.DTOs.Recommendations;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for managing medical, physical activity, and operational recommendations.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Retrieves health and operational recommendations for an astronaut.
    /// </summary>
    Task<IReadOnlyList<RecommendationDto>> GetRecommendationsForAstronautAsync(int astronautId, bool pendingOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all recommendations across the crew for monitor oversight.
    /// </summary>
    Task<IReadOnlyList<RecommendationDto>> GetAllRecommendationsAsync(bool pendingOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single recommendation by its unique database identifier.
    /// </summary>
    Task<RecommendationDto?> GetRecommendationByIdAsync(int recommendationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a new medical or lifestyle recommendation for an astronaut with an explanatory reason.
    /// </summary>
    Task<RecommendationDto> CreateRecommendationAsync(int astronautId, CreateRecommendationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the acknowledgment or completion status of an astronaut recommendation.
    /// </summary>
    Task<RecommendationDto> UpdateStatusAsync(int recommendationId, int astronautId, UpdateRecommendationStatusRequest request, CancellationToken cancellationToken = default);
}

