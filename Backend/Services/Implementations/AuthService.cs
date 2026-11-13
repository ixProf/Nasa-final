using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AstronautHealthMonitoring.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Lookup by username or email
        var user = await _userManager.FindByNameAsync(request.Username)
            ?? await _userManager.FindByEmailAsync(request.Username);

        // Generic error message prevents username harvesting / user enumeration
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException("Account temporarily locked due to multiple failed login attempts. Please try again later.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        // Reset failed attempt count upon successful authentication
        await _userManager.ResetAccessFailedCountAsync(user);

        return await GenerateAuthResponseForUserAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var presentedHash = _tokenService.HashToken(request.RefreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == presentedHash, cancellationToken);

        if (existingToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // Token reuse / retry check
        if (existingToken.IsRevoked)
        {
            var graceWindowSeconds = double.TryParse(_configuration["Jwt:RefreshTokenGraceWindowSeconds"], out var g) ? g : 30.0;
            var isWithinGraceWindow = existingToken.RevokedAt.HasValue
                && (DateTime.UtcNow - existingToken.RevokedAt.Value).TotalSeconds <= graceWindowSeconds;

            if (isWithinGraceWindow && !string.IsNullOrEmpty(existingToken.ReplacedByTokenHash))
            {
                // Retry within grace window (e.g. lost response on flaky network link)
                var replacementToken = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.TokenHash == existingToken.ReplacedByTokenHash, cancellationToken);

                // If the replacement token is still active, supersede it with a fresh token to recover session
                if (replacementToken != null && replacementToken.IsActive)
                {
                    replacementToken.RevokedAt = DateTime.UtcNow;

                    var retryRawToken = _tokenService.GenerateRefreshToken();
                    var retryHash = _tokenService.HashToken(retryRawToken);
                    replacementToken.ReplacedByTokenHash = retryHash;
                    existingToken.ReplacedByTokenHash = retryHash;

                    var retryTokenEntity = new RefreshToken
                    {
                        UserId = existingToken.UserId,
                        TokenHash = retryHash,
                        ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.RefreshTokens.Add(retryTokenEntity);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return await BuildAuthResponseAsync(existingToken.User, retryRawToken, cancellationToken);
                }
            }

            // Outside the grace window or invalid token chain: Token reuse detected!
            // Revoke all active sessions for this user family
            var activeTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == existingToken.UserId && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Invalid refresh token. Potential token reuse detected.");
        }

        if (existingToken.IsExpired)
        {
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");
        }

        // Rotate token: revoke old token and issue a fresh successor
        existingToken.RevokedAt = DateTime.UtcNow;

        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newHash = _tokenService.HashToken(newRawRefreshToken);
        existingToken.ReplacedByTokenHash = newHash;

        var replacementTokenEntity = new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newHash,
            ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(replacementTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildAuthResponseAsync(existingToken.User, newRawRefreshToken, cancellationToken);
    }

    public async Task<bool> LogoutAsync(string? rawRefreshToken, string? userId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            var hash = _tokenService.HashToken(rawRefreshToken);
            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

            if (token != null && token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        // Also revoke all active tokens for the user if userId provided
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var userTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in userTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? UserRoles.Astronaut;

        Astronaut? astronaut = null;
        if (role == UserRoles.Astronaut)
        {
            astronaut = await _dbContext.Astronauts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == user.Id, cancellationToken);
        }

        return new CurrentUserDto
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = role,
            AstronautId = astronaut?.Id,
            AstronautCode = astronaut?.AstronautCode
        };
    }

    private async Task<AuthResponse> GenerateAuthResponseForUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildAuthResponseAsync(user, rawRefreshToken, cancellationToken);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, string rawRefreshToken, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? UserRoles.Astronaut;

        Astronaut? astronaut = null;
        if (role == UserRoles.Astronaut)
        {
            astronaut = await _dbContext.Astronauts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == user.Id, cancellationToken);
        }

        var accessToken = _tokenService.GenerateAccessToken(user, role, astronaut?.Id);

        return new AuthResponse
        {
            Token = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Role = role,
            FullName = user.FullName,
            AstronautId = astronaut?.Id,
            AstronautCode = astronaut?.AstronautCode
        };
    }
}
