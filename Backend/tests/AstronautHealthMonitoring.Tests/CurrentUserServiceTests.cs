using System.Security.Claims;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public void CurrentUserService_WhenAuthenticatedAstronaut_ResolvesClaimsCorrectly()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "astro-user-01"),
            new(ClaimTypes.Role, UserRoles.Astronaut),
            new("full_name", "Commander Alex Vance"),
            new("astronaut_id", "7")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUserService = new CurrentUserService(httpContextAccessor);

        Assert.True(currentUserService.IsAuthenticated);
        Assert.Equal("astro-user-01", currentUserService.UserId);
        Assert.Equal(UserRoles.Astronaut, currentUserService.Role);
        Assert.Equal("Commander Alex Vance", currentUserService.FullName);
        Assert.Equal(7, currentUserService.AstronautId);
        Assert.True(currentUserService.IsInRole(UserRoles.Astronaut));
        Assert.False(currentUserService.IsInRole(UserRoles.Monitor));
    }

    [Fact]
    public void CurrentUserService_WhenAuthenticatedMonitor_ResolvesNullAstronautId()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "monitor-user-02"),
            new(ClaimTypes.Role, UserRoles.Monitor),
            new("full_name", "Casey Rivera")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUserService = new CurrentUserService(httpContextAccessor);

        Assert.True(currentUserService.IsAuthenticated);
        Assert.Equal("monitor-user-02", currentUserService.UserId);
        Assert.Equal(UserRoles.Monitor, currentUserService.Role);
        Assert.Equal("Casey Rivera", currentUserService.FullName);
        Assert.Null(currentUserService.AstronautId);
        Assert.True(currentUserService.IsInRole(UserRoles.Monitor));
        Assert.False(currentUserService.IsInRole(UserRoles.Astronaut));
    }

    [Fact]
    public void CurrentUserService_WhenUnauthenticated_ReturnsNullAndFalse()
    {
        var httpContext = new DefaultHttpContext(); // No user authenticated
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUserService = new CurrentUserService(httpContextAccessor);

        Assert.False(currentUserService.IsAuthenticated);
        Assert.Null(currentUserService.UserId);
        Assert.Null(currentUserService.Role);
        Assert.Null(currentUserService.AstronautId);
        Assert.Null(currentUserService.FullName);
        Assert.False(currentUserService.IsInRole(UserRoles.Astronaut));
    }
}

