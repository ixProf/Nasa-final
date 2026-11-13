using Microsoft.AspNetCore.Identity;

namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Custom Identity user representing an authenticated crew member or ground station monitor.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's formal full name (e.g., "Casey Rivera", "Morgan Vance").
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional 1-to-1 link to an Astronaut profile (for users in the Astronaut role).
    /// </summary>
    public Astronaut? Astronaut { get; set; }

    /// <summary>
    /// Refresh tokens issued for this user across sessions and devices.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

