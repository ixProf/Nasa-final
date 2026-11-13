namespace AstronautHealthMonitoring.DTOs.Auth;

/// <summary>
/// Authorization policies configured across the application.
/// </summary>
public static class AuthPolicies
{
    public const string AstronautOnly = "AstronautOnly";
    public const string MonitorOnly = "MonitorOnly";
    public const string Authenticated = "Authenticated";
}

