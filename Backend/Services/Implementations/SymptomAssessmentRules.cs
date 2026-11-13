using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Services.Implementations;

// Deterministic demo rules from Final Edits; never a diagnostic model.
public static class SymptomAssessmentRules
{
    public static SymptomAssessmentResponseDto Evaluate(
        EvaluateSymptomsRequest request, IReadOnlyList<Symptom> symptoms,
        IReadOnlyList<VitalSignReading> latestReadings, double? sleepHours)
    {
        var result = new SymptomAssessmentResponseDto();
        var codes = symptoms.Select(s => s.Code.ToUpperInvariant()).ToHashSet();
        result.RedFlags = symptoms.Where(s => s.IsRedFlag).Select(s => s.Name).ToList();
        if (result.RedFlags.Count > 0)
        {
            result.Level = 3;
            result.ShouldEscalate = true;
            result.EscalationReason = $"Red flag symptom(s) reported: {string.Join(", ", result.RedFlags)}.";
            result.Reasons.Add(result.EscalationReason);
            result.RecommendedActions.Add("Stop activity, secure your posture, follow the emergency protocol and contact the medical officer immediately.");
            return result;
        }

        var score = request.Severity switch { "severe" => 3, "moderate" => 2, _ => 1 };
        result.Reasons.Add($"Reported {request.Severity} symptoms: {string.Join(", ", symptoms.Select(s => s.Name))}.");
        if (request.DurationHours > 24)
        {
            score++;
            result.Reasons.Add($"Symptoms have lasted {request.DurationHours:0.#} hours.");
        }
        foreach (var reading in latestReadings)
        {
            var indicator = reading.VitalSignType.Code;
            var correlated = indicator switch
            {
                "BP" => codes.Contains("DIZZINESS") && (reading.Value < 90 || reading.SecondaryValue < 60),
                "TEMP" => codes.Contains("CHILLS") && reading.Value > 37.3,
                "GLUCOSE" => (codes.Contains("SWEATING") || codes.Contains("TREMOR")) && reading.Value < 70,
                "SPO2" => codes.Contains("RESPIRATORY_IRRITATION") && reading.Value < 95,
                _ => false
            };
            if (!correlated) continue;
            score += 2;
            result.Reasons.Add($"Related {indicator} reading is {reading.Value:0.#}{(reading.SecondaryValue.HasValue ? $"/{reading.SecondaryValue:0.#}" : "")} {reading.VitalSignType.Unit} ({reading.RecordedAt:u}).");
        }
        if (codes.Contains("BLURRED_VISION") && codes.Contains("HEADACHE") && request.DurationHours > 24)
        {
            score += 2;
            result.Reasons.Add("Visual symptoms accompany a persistent headache.");
        }
        if ((codes.Contains("INCREASED_STRESS") || codes.Contains("MOOD_CHANGES")) && sleepHours < 5)
        {
            score += 2;
            result.Reasons.Add($"Latest sleep duration was {sleepHours:0.#} hours alongside psychological distress.");
            result.RecommendedActions.Add("Seek crew support and contact the psychological support specialist.");
        }
        result.Level = score >= 3 ? 2 : 1;
        result.RecommendedActions.Add(result.Level == 2
            ? "Re-measure relevant indicators, log symptoms and contact the medical officer for review."
            : "Rest, maintain hydration according to your mission plan, and re-measure. Report persistent or worsening symptoms.");
        return result;
    }
}
