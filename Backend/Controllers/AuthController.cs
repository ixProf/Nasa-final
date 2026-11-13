using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService _authService, ICurrentUserService currentUserService)
    {
        this._authService = _authService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Authenticates a user with username/email and password, returning JWT access and refresh tokens.
    /// Rate limited to mitigate brute-force and credential-stuffing attacks.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginLimiter")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Validates an active refresh token, issues rotated tokens, and invalidates the previous refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Revokes active refresh token(s) for the authenticated session upon user logout.
    /// </summary>
    [HttpPost("logout")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        await _authService.LogoutAsync(request?.RefreshToken, userId, cancellationToken);
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Returns the identity profile, roles, and linked astronaut metadata for the current authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity could not be resolved from token claims." });
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User account not found." });
        }

        return Ok(user);
    }
}

