using System.Security.Claims;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AstronautHealthMonitoring.Services.Implementations;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public string? FullName => User?.FindFirstValue("full_name");

    public int? AstronautId
    {
        get
        {
            var claimValue = User?.FindFirstValue("astronaut_id");
            return int.TryParse(claimValue, out var parsed) ? parsed : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}

