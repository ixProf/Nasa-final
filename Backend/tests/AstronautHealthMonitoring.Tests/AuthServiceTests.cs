using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class AuthServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        _dbContext = provider.GetRequiredService<AppDbContext>();
        _userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "__TEST_MOCK_JWT_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_32_BYTES__",
            ["Jwt:Issuer"] = "AstronautHealthMonitoring",
            ["Jwt:Audience"] = "AstronautHealthMonitoringClient",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "30",
            ["Jwt:RefreshTokenGraceWindowSeconds"] = "30"
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        _tokenService = new TokenService(_configuration);

        _authService = new AuthService(_userManager, _dbContext, _tokenService, _configuration);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndClaims()
    {
        var user = new ApplicationUser
        {
            UserName = "test.pilot",
            Email = "pilot@mission.space",
            FullName = "Morgan Vance",
            EmailConfirmed = true
        };
        var createResult = await _userManager.CreateAsync(user, "Password123!");
        Assert.True(createResult.Succeeded);

        var astronaut = new Astronaut
        {
            AstronautCode = "AST-999",
            FirstName = "Morgan",
            LastName = "Vance",
            FullName = "Morgan Vance",
            UserId = user.Id
        };
        _dbContext.Astronauts.Add(astronaut);
        await _dbContext.SaveChangesAsync();

        var response = await _authService.LoginAsync(new LoginRequest
        {
            Username = "test.pilot",
            Password = "Password123!"
        });

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal("Morgan Vance", response.FullName);
        Assert.Equal(astronaut.Id, response.AstronautId);
        Assert.Equal("AST-999", response.AstronautCode);

        // Verify refresh token persisted in DB
        var hash = _tokenService.HashToken(response.RefreshToken);
        var tokenEntity = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);
        Assert.NotNull(tokenEntity);
        Assert.True(tokenEntity.IsActive);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorized_GenericMessage()
    {
        var user = new ApplicationUser
        {
            UserName = "cadet.smith",
            Email = "smith@mission.space",
            FullName = "Cadet Smith"
        };
        await _userManager.CreateAsync(user, "CorrectPassword1!");

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest
            {
                Username = "cadet.smith",
                Password = "WrongPassword!"
            }));

        Assert.Equal("Invalid credentials.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUser_ThrowsUnauthorized_GenericMessage()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest
            {
                Username = "nonexistent.user",
                Password = "AnyPassword123!"
            }));

        Assert.Equal("Invalid credentials.", ex.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_RotatesToken_AndRevokesOldToken()
    {
        var user = new ApplicationUser
        {
            UserName = "astro.refresh",
            Email = "refresh@mission.space",
            FullName = "Astro Refresher"
        };
        await _userManager.CreateAsync(user, "ValidPass123!");

        var rawInitialToken = _tokenService.GenerateRefreshToken();
        var initialHash = _tokenService.HashToken(rawInitialToken);

        var existingTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = initialHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefreshTokens.Add(existingTokenEntity);
        await _dbContext.SaveChangesAsync();

        // Rotate
        var response = await _authService.RefreshTokenAsync(new RefreshTokenRequest
        {
            Token = "dummy-old-access-token",
            RefreshToken = rawInitialToken
        });

        Assert.NotNull(response);
        Assert.NotEqual(rawInitialToken, response.RefreshToken);

        // Verify old token is revoked and replacement hash is recorded
        var oldTokenInDb = await _dbContext.RefreshTokens.FirstAsync(rt => rt.TokenHash == initialHash);
        Assert.NotNull(oldTokenInDb.RevokedAt);
        Assert.True(oldTokenInDb.IsRevoked);
        Assert.NotNull(oldTokenInDb.ReplacedByTokenHash);

        // Verify new token exists and is active
        var newHash = _tokenService.HashToken(response.RefreshToken);
        Assert.Equal(newHash, oldTokenInDb.ReplacedByTokenHash);

        var newTokenInDb = await _dbContext.RefreshTokens.FirstAsync(rt => rt.TokenHash == newHash);
        Assert.True(newTokenInDb.IsActive);
        Assert.Null(newTokenInDb.RevokedAt);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithinGraceWindow_DoesNotRevokeFamily_AndReturnsNewToken()
    {
        var user = new ApplicationUser
        {
            UserName = "flaky.network.user",
            Email = "flaky@mission.space",
            FullName = "Flaky Network Astronaut"
        };
        await _userManager.CreateAsync(user, "ValidPass123!");

        // Token T1 was legitimately rotated to T2 just 5 seconds ago (within 30s grace window)
        var rawT1 = "initial-token-t1";
        var hashT1 = _tokenService.HashToken(rawT1);

        var rawT2 = "replacement-token-t2";
        var hashT2 = _tokenService.HashToken(rawT2);

        var tokenT1 = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hashT1,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            RevokedAt = DateTime.UtcNow.AddSeconds(-5), // Rotated 5 seconds ago (within 30s grace window)
            ReplacedByTokenHash = hashT2
        };

        var tokenT2 = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hashT2,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddSeconds(-5) // Active replacement token
        };

        _dbContext.RefreshTokens.AddRange(tokenT1, tokenT2);
        await _dbContext.SaveChangesAsync();

        // Client retries with T1 because response with T2 was lost due to network glitch
        var response = await _authService.RefreshTokenAsync(new RefreshTokenRequest
        {
            Token = "dummy-jwt",
            RefreshToken = rawT1
        });

        // Succeeded without throwing!
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.NotEqual(rawT1, response.RefreshToken);
        Assert.NotEqual(rawT2, response.RefreshToken);

        // Verify T2 was superseded and new token T3 is active
        var hashT3 = _tokenService.HashToken(response.RefreshToken);
        var tokenT3 = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hashT3);
        Assert.NotNull(tokenT3);
        Assert.True(tokenT3.IsActive);

        // Verify user session family was NOT revoked
        Assert.False(await _dbContext.RefreshTokens.AllAsync(rt => rt.UserId == user.Id && rt.RevokedAt != null));
    }

    [Fact]
    public async Task RefreshTokenAsync_OutsideGraceWindow_RevokesAllActiveTokens_AndThrowsUnauthorized()
    {
        var user = new ApplicationUser
        {
            UserName = "victim.user",
            Email = "victim@mission.space",
            FullName = "Victim User"
        };
        await _userManager.CreateAsync(user, "ValidPass123!");

        // Token was rotated 60 seconds ago (outside 30-second grace window)
        var rawCompromisedToken = "stolen-compromised-token";
        var compromisedHash = _tokenService.HashToken(rawCompromisedToken);

        var revokedToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = compromisedHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = DateTime.UtcNow.AddSeconds(-60), // Outside grace window
            ReplacedByTokenHash = "some-replacement-hash"
        };

        // User currently has an active valid token
        var activeToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken("current-active-token"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddSeconds(-60)
        };

        _dbContext.RefreshTokens.AddRange(revokedToken, activeToken);
        await _dbContext.SaveChangesAsync();

        // An attacker tries to replay the old token outside the grace window
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            {
                Token = "old-jwt",
                RefreshToken = rawCompromisedToken
            }));

        Assert.Contains("reuse detected", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Verify all active tokens for this user family were revoked
        var activeTokenAfter = await _dbContext.RefreshTokens.FirstAsync(rt => rt.TokenHash == activeToken.TokenHash);
        Assert.NotNull(activeTokenAfter.RevokedAt);
        Assert.True(activeTokenAfter.IsRevoked);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsUnauthorized()
    {
        var user = new ApplicationUser
        {
            UserName = "expired.user",
            Email = "expired@mission.space",
            FullName = "Expired User"
        };
        await _userManager.CreateAsync(user, "ValidPass123!");

        var rawToken = "expired-token-xyz";
        var hash = _tokenService.HashToken(rawToken);

        var expiredToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-31)
        };
        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            {
                Token = "old-jwt",
                RefreshToken = rawToken
            }));

        Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogoutAsync_RevokesActiveRefreshToken()
    {
        var rawToken = "active-logout-token";
        var hash = _tokenService.HashToken(rawToken);

        var token = new RefreshToken
        {
            UserId = "user-to-logout",
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();

        var result = await _authService.LogoutAsync(rawToken, "user-to-logout");

        Assert.True(result);
        var tokenInDb = await _dbContext.RefreshTokens.FirstAsync(rt => rt.TokenHash == hash);
        Assert.NotNull(tokenInDb.RevokedAt);
        Assert.True(tokenInDb.IsRevoked);
    }
}
