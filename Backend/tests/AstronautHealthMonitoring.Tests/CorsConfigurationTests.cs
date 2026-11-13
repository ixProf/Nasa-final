using AstronautHealthMonitoring.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AstronautHealthMonitoring.Tests;

public class CorsConfigurationTests
{
    [Fact]
    public void BrowserPreflight_AllowsConfiguredOriginWithCredentials_RejectsOtherOrigins()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Cors:AllowedOrigins:0"] = "http://localhost:3000/",
            ["Cors:AllowedOrigins:1"] = "https://frontend.example"
        }).Build();
        var policy = CorsConfiguration.CreatePolicy(config);
        var service = new CorsService(Options.Create(new CorsOptions()), NullLoggerFactory.Instance);
        foreach (var origin in new[] {"http://localhost:3000","https://frontend.example","https://untrusted.example"})
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "OPTIONS";
            context.Request.Headers.Origin = origin;
            context.Request.Headers.AccessControlRequestMethod = "POST";
            context.Request.Headers.AccessControlRequestHeaders = "authorization,content-type";
            var result = service.EvaluatePolicy(context,policy);
            Assert.True(result.IsPreflightRequest);
            Assert.Equal(origin != "https://untrusted.example",result.IsOriginAllowed);
            if (result.IsOriginAllowed)
            {
                Assert.True(result.SupportsCredentials);
                Assert.Contains("authorization",result.AllowedHeaders);
                Assert.Contains("POST",result.AllowedMethods);
            }
        }
    }

    [Fact]
    public void WildcardOrigin_IsRejectedWhenCredentialsAreEnabled()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        { ["Cors:AllowedOrigins:0"] = "https://*.example" }).Build();
        Assert.Throws<InvalidOperationException>(() => CorsConfiguration.CreatePolicy(config));
    }
}
