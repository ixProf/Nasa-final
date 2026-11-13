namespace AstronautHealthMonitoring.Models.Entities;

/// <summary>
/// Cryptographically hashed refresh token supporting session continuity, token rotation, and revocation.
/// Plain-text token strings are never stored in the database.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>
    /// Hex-encoded SHA-256 hash of the issued refresh token.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Identity user ID who owns this token session.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Timestamp when this token was revoked (e.g., during logout or rotation).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Hash of the successor token issued when this token was rotated (for reuse detection).
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;
}

