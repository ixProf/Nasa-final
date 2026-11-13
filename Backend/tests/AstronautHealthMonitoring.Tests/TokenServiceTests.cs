using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "__TEST_MOCK_JWT_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_32_BYTES__",
            ["Jwt:Issuer"] = "AstronautHealthMonitoring",
            ["Jwt:Audience"] = "AstronautHealthMonitoringClient",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "30"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    [Fact]
    public void GenerateAccessToken_ForAstronaut_ContainsExpectedClaims_IncludingAstronautId()
    {
        var service = new TokenService(_configuration);
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = "astro.vance",
            Email = "alex.vance@mission.space",
            FullName = "Alex Vance"
        };

        var tokenString = service.GenerateAccessToken(user, UserRoles.Astronaut, astronautId: 42);

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "AstronautHealthMonitoring",
            ValidateAudience = true,
            ValidAudience = "AstronautHealthMonitoringClient",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("__TEST_MOCK_JWT_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_32_BYTES__")),
            ValidateLifetime = false
        };

        var principal = handler.ValidateToken(tokenString, validationParameters, out var validatedToken);
        var jwt = Assert.IsType<JwtSecurityToken>(validatedToken);

        Assert.Equal("AstronautHealthMonitoring", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "AstronautHealthMonitoringClient");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal("user-123", userIdClaim.Value);

        var roleClaim = principal.FindFirst(ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal(UserRoles.Astronaut, roleClaim.Value);

        var nameClaim = principal.FindFirst(ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("astro.vance", nameClaim.Value);

        var fullNameClaim = principal.FindFirst("full_name");
        Assert.NotNull(fullNameClaim);
        Assert.Equal("Alex Vance", fullNameClaim.Value);

        var astroIdClaim = principal.FindFirst("astronaut_id");
        Assert.NotNull(astroIdClaim);
        Assert.Equal("42", astroIdClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_ForMonitor_ExcludesAstronautIdClaim()
    {
        var service = new TokenService(_configuration);
        var user = new ApplicationUser
        {
            Id = "monitor-999",
            UserName = "monitor.rivera",
            Email = "casey.rivera@missioncontrol.space",
            FullName = "Casey Rivera"
        };

        var tokenString = service.GenerateAccessToken(user, UserRoles.Monitor, astronautId: null);

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "AstronautHealthMonitoring",
            ValidateAudience = true,
            ValidAudience = "AstronautHealthMonitoringClient",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("__TEST_MOCK_JWT_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_32_BYTES__")),
            ValidateLifetime = false
        };

        var principal = handler.ValidateToken(tokenString, validationParameters, out _);

        var roleClaim = principal.FindFirst(ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal(UserRoles.Monitor, roleClaim.Value);

        var astroIdClaim = principal.FindFirst("astronaut_id");
        Assert.Null(astroIdClaim);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsHighEntropyUniqueString()
    {
        var service = new TokenService(_configuration);

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.False(string.IsNullOrWhiteSpace(token2));
        Assert.NotEqual(token1, token2);

        // Verify valid Base64
        var bytes = Convert.FromBase64String(token1);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void HashToken_ReturnsDeterministicSha256HexString()
    {
        var service = new TokenService(_configuration);
        var rawToken = "sample-raw-refresh-token-value-xyz";

        var hash1 = service.HashToken(rawToken);
        var hash2 = service.HashToken(rawToken);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // 256 bits = 64 hex characters
        Assert.True(hash1.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')));
    }
}
