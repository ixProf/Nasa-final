using AstronautHealthMonitoring.DTOs.SymptomGuidance;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for symptom catalogs, space condition guidance, and non-diagnostic topic evaluation.
/// </summary>
public interface ISymptomGuidanceService
{
    /// <summary>
    /// Retrieves the full catalog of observable signs and subjective symptoms.
    /// </summary>
    Task<IReadOnlyList<SymptomDto>> GetAllSymptomsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves spaceflight adaptation conditions and non-diagnostic guidance topics, optionally filtered by search text.
    /// </summary>
    Task<IReadOnlyList<SpaceConditionDto>> GetAllConditionsAsync(string? search = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates reported symptom codes and returns ranked possible related health topics using non-diagnostic associations.
    /// Escalates to emergency alerts if red-flag symptoms are reported.
    /// </summary>
    Task<SymptomAssessmentResponseDto> EvaluateSymptomsAsync(EvaluateSymptomsRequest request, CancellationToken cancellationToken = default);
}
