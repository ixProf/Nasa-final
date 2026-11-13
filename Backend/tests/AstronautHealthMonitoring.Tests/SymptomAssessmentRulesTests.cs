using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;

namespace AstronautHealthMonitoring.Tests;

public class SymptomAssessmentRulesTests
{
    [Fact]
    public void RequiredCases_HaveExpectedLevelsAndDifferentReasons()
    {
        Symptom S(string code, bool red = false) => new() {Code=code,Name=code,IsRedFlag=red};
        SymptomAssessmentResponseDto Run(string severity, double duration, Symptom[] symptoms,
            VitalSignReading[]? vitals = null, double? sleep = null) => SymptomAssessmentRules.Evaluate(
                new EvaluateSymptomsRequest {Severity=severity,DurationHours=duration}, symptoms,
                vitals ?? [],sleep);

        var results = new[] {
            Run("mild",1,[S("HEADACHE")]),
            Run("moderate",2,[S("DIZZINESS")],[new VitalSignReading {
                Value=85,SecondaryValue=55,RecordedAt=DateTime.UtcNow,
                VitalSignType=new VitalSignType {Code="BP",Unit="mmHg"}}]),
            Run("mild",1,[S("CHEST_PAIN",true)]),
            Run("mild",30,[S("BLURRED_VISION"),S("HEADACHE")]),
            Run("severe",2,[S("INCREASED_STRESS")],sleep:4.5)
        };
        Assert.Equal(new[] {1,2,3,2,2},results.Select(r=>r.Level));
        Assert.Equal(5,results.Select(r=>string.Join(" ",r.Reasons)).Distinct().Count());
        Assert.All(results,r=> { Assert.NotEmpty(r.Reasons); Assert.NotEmpty(r.RecommendedActions); });
        Assert.True(results[2].ShouldEscalate);
        Assert.Contains(results[4].RecommendedActions,a=>a.Contains("psychological"));
    }

    [Fact]
    public void AssessmentJson_DoesNotExposeConditionMatches()
    {
        var response = new SymptomAssessmentResponseDto {Matches=[new() {ConditionName="Private educational reference"}]};
        var json = System.Text.Json.JsonSerializer.Serialize(response);
        Assert.DoesNotContain("Private educational reference",json);
        using var document=System.Text.Json.JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("Matches",out _));
        Assert.True(document.RootElement.TryGetProperty("ConditionMatches",out _));
    }
}
