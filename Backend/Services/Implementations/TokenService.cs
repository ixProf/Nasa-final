using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AstronautHealthMonitoring.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(ApplicationUser user, string role, int? astronautId = null)
    {
        var keyString = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured. Set 'Jwt:Key' via user-secrets or environment variables.");

        var issuer = _configuration["Jwt:Issuer"] ?? "AstronautHealthMonitoring";
        var audience = _configuration["Jwt:Audience"] ?? "AstronautHealthMonitoringClient";
        var expirationMinutes = GetAccessTokenExpirationMinutes();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, role),
            new("full_name", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // astronaut_id is included ONLY for astronauts who have an associated Astronaut record
        if (role == UserRoles.Astronaut && astronautId.HasValue)
        {
            claims.Add(new Claim("astronaut_id", astronautId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashToken(string rawToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public DateTime GetAccessTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes());
    }

    public DateTime GetRefreshTokenExpiration()
    {
        var days = double.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var parsed) ? parsed : 30.0;
        return DateTime.UtcNow.AddDays(days);
    }

    private double GetAccessTokenExpirationMinutes()
    {
        return double.TryParse(_configuration["Jwt:AccessTokenExpirationMinutes"], out var parsed) ? parsed : 60.0;
    }
}

