using System.Net.Http.Json;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;

namespace AstronautHealthMonitoring.Services.Implementations;

/// <summary>Optional adapter for a separately trained and validated probability model.</summary>
public class RiskProbabilityPredictor(HttpClient http,IConfiguration config,ILogger<RiskProbabilityPredictor> logger)
{
    public async Task ApplyAsync(SymptomAssessmentResponseDto result,CancellationToken ct)
    {
        var settings=config.GetSection("RiskProbabilityModel");
        if(!settings.GetValue("Enabled",false))return;
        var endpoint=settings["Endpoint"];
        if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri) || uri.Scheme!="https" || string.IsNullOrWhiteSpace(settings["ValidationReference"]))
        {
            logger.LogWarning("Probability model requires an HTTPS endpoint and a validation reference");return;
        }
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            // No astronaut identity, names or free text is sent to the model.
            using var request=new HttpRequestMessage(HttpMethod.Post,uri){Content=JsonContent.Create(new {
                engineVersion=result.EngineVersion,
                symptoms=result.SymptomFindings.Select(s=>new{s.Code,s.Severity,s.DurationHours}),
                conditionCodes=result.ConditionMatches.Select(c=>c.ConditionCode)
            })};
            if(!string.IsNullOrWhiteSpace(settings["ApiKey"]))request.Headers.Authorization=new("Bearer",settings["ApiKey"]);
            using var response=await http.SendAsync(request,timeout.Token);response.EnsureSuccessStatusCode();
            var model=await response.Content.ReadFromJsonAsync<ModelResult>(cancellationToken:timeout.Token);
            if(model is null || string.IsNullOrWhiteSpace(model.ModelVersion) || model.ModelVersion!=settings["ModelVersion"] || model.Predictions is null)throw new InvalidOperationException("Unexpected probability model version or schema.");
            if(model.Predictions.Any(p=>p.ProbabilityPercent is <0 or >100) || model.Predictions.Select(p=>p.ConditionCode).Distinct().Count()!=model.Predictions.Count)throw new InvalidOperationException("Invalid model predictions.");
            foreach(var match in result.ConditionMatches)
            {
                var prediction=model.Predictions.SingleOrDefault(p=>p.ConditionCode==match.ConditionCode);
                if(prediction is not null){match.ProbabilityPercent=prediction.ProbabilityPercent;match.ProbabilityStatus="model_prediction";}
            }
        }
        catch(Exception error) when(!ct.IsCancellationRequested)
        {
            logger.LogWarning(error,"Probability prediction unavailable; deterministic findings remain available");
            foreach(var match in result.ConditionMatches)match.ProbabilityStatus="model_unavailable";
        }
    }
    public record Prediction([property:System.Text.Json.Serialization.JsonRequired]string ConditionCode,[property:System.Text.Json.Serialization.JsonRequired]decimal ProbabilityPercent);
    public record ModelResult([property:System.Text.Json.Serialization.JsonRequired]string ModelVersion,[property:System.Text.Json.Serialization.JsonRequired]List<Prediction> Predictions);
}
