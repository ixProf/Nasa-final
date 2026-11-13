namespace AstronautHealthMonitoring.DTOs.Auth;

/// <summary>
/// Profile information for the currently authenticated user session.
/// </summary>
public class CurrentUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? AstronautId { get; set; }
    public string? AstronautCode { get; set; }
}

