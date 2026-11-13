namespace AstronautHealthMonitoring.DTOs.Auth;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Astronaut"; // "Astronaut" or "Monitor"
    public string FullName { get; set; } = string.Empty;
    public string? AstronautCode { get; set; }
}

