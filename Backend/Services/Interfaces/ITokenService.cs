using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for generating JWT access tokens and cryptographically random, hashed refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT access token with user identification, role, and astronaut claims.
    /// </summary>
    string GenerateAccessToken(ApplicationUser user, string role, int? astronautId = null);

    /// <summary>
    /// Generates a high-entropy cryptographically random raw refresh token string.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Computes the SHA-256 hash of a raw refresh token string for secure database persistence.
    /// </summary>
    string HashToken(string rawToken);

    /// <summary>
    /// Returns the expiration timestamp for a newly generated access token.
    /// </summary>
    DateTime GetAccessTokenExpiration();

    /// <summary>
    /// Returns the expiration timestamp for a newly generated refresh token.
    /// </summary>
    DateTime GetRefreshTokenExpiration();
}

