using Microsoft.AspNetCore.Cors.Infrastructure;

namespace AstronautHealthMonitoring.Configuration;

public static class CorsConfiguration
{
    public static CorsPolicy CreatePolicy(IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5173"];
        origins = origins.Select(origin => origin.Trim().TrimEnd('/')).Distinct().ToArray();
        if (origins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https") || uri.AbsolutePath != "/"
            || uri.Host.Contains('*') || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)))
            throw new InvalidOperationException("Cors:AllowedOrigins must contain exact HTTP(S) origins without paths or wildcards.");
        return new CorsPolicyBuilder().WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials().Build();
    }
}
