namespace AstronautHealthMonitoring.DTOs.Auth;

/// <summary>
/// Optional request payload for revoking a specific refresh token during logout.
/// </summary>
public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

