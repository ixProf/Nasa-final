namespace AstronautHealthMonitoring.Services.Interfaces;

/// <summary>
/// Provides strongly-typed access to identity claims of the currently authenticated request context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Unique identity user ID (from NameIdentifier claim).
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// User role ("Astronaut" or "Monitor").
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Associated Astronaut database ID (present only for users with the Astronaut role).
    /// </summary>
    int? AstronautId { get; }

    /// <summary>
    /// User's formal full name.
    /// </summary>
    string? FullName { get; }

    /// <summary>
    /// True if the current HTTP context represents an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks whether the current user possesses the specified role.
    /// </summary>
    bool IsInRole(string role);
}

