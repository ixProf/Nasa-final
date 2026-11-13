using System.Net;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstronautHealthMonitoring.Tests;

public class RiskProbabilityPredictorTests
{
    private class Handler(string json):HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){Calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json,System.Text.Encoding.UTF8,"application/json")});}
    }
    private static IConfiguration Config(bool enabled=true)=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
        ["RiskProbabilityModel:Enabled"]=enabled.ToString(),["RiskProbabilityModel:Endpoint"]="https://model.example/predict",
        ["RiskProbabilityModel:ModelVersion"]="validated-v1",["RiskProbabilityModel:ValidationReference"]="Operator-provided validation study"
    }).Build();
    [Fact] public async Task ConfiguredModel_CanSupplyProbabilityButCannotChangeUrgency()
    {
        var handler=new Handler("{\"modelVersion\":\"validated-v1\",\"predictions\":[{\"conditionCode\":\"SLEEP_DEFICIENCY\",\"probabilityPercent\":62.5}]}");
        var result=new SymptomAssessmentResponseDto{Level=3,ConditionMatches=[new(){ConditionCode="SLEEP_DEFICIENCY",AssociationIndex=79}]};
        await new RiskProbabilityPredictor(new HttpClient(handler),Config(),NullLogger<RiskProbabilityPredictor>.Instance).ApplyAsync(result,default);
        Assert.Equal(62.5m,result.ConditionMatches[0].ProbabilityPercent);Assert.Equal(3,result.Level);Assert.Equal(79,result.ConditionMatches[0].AssociationIndex);
    }
    [Fact] public async Task MissingOrInvalidPredictions_NeverBecomeInventedZeroProbabilities()
    {
        foreach(var json in new[]{"{}","{\"modelVersion\":\"validated-v1\",\"predictions\":[{\"conditionCode\":\"SLEEP_DEFICIENCY\"}]}","{\"modelVersion\":\"validated-v1\",\"predictions\":[{\"conditionCode\":\"SLEEP_DEFICIENCY\",\"probabilityPercent\":101}]}"})
        {
            var result=new SymptomAssessmentResponseDto{ConditionMatches=[new(){ConditionCode="SLEEP_DEFICIENCY"}]};
            await new RiskProbabilityPredictor(new HttpClient(new Handler(json)),Config(),NullLogger<RiskProbabilityPredictor>.Instance).ApplyAsync(result,default);
            Assert.Null(result.ConditionMatches[0].ProbabilityPercent);Assert.Equal("model_unavailable",result.ConditionMatches[0].ProbabilityStatus);
        }
    }
    [Fact] public async Task DisabledModel_PerformsNoExternalRequest()
    {
        var handler=new Handler("{}");var result=new SymptomAssessmentResponseDto{ConditionMatches=[new(){ConditionCode="SLEEP_DEFICIENCY"}]};
        await new RiskProbabilityPredictor(new HttpClient(handler),Config(false),NullLogger<RiskProbabilityPredictor>.Instance).ApplyAsync(result,default);
        Assert.Equal(0,handler.Calls);Assert.Null(result.ConditionMatches[0].ProbabilityPercent);
    }
}
