using AstronautHealthMonitoring.DTOs.Auth;

namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Service contract for authentication, credential verification, token rotation, and session management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates credentials, validates lockout status, and issues access/refresh tokens.
    /// Employs generic error responses to prevent user enumeration.
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token, executes rotation, detects reuse attacks, and issues replacement tokens.
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the specified active refresh token or all tokens for the user session on logout.
    /// </summary>
    Task<bool> LogoutAsync(string? rawRefreshToken, string? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves user profile and identity claims for the authenticated user session.
    /// </summary>
    Task<CurrentUserDto?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
}

