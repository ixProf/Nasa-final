using System.Security.Claims;
using AstronautHealthMonitoring.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class AuthorizationPolicyTests
{
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationPolicyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AstronautOnly, policy => policy.RequireRole(UserRoles.Astronaut));
            options.AddPolicy(AuthPolicies.MonitorOnly, policy => policy.RequireRole(UserRoles.Monitor));
            options.AddPolicy(AuthPolicies.Authenticated, policy => policy.RequireAuthenticatedUser());
        });

        var provider = services.BuildServiceProvider();
        _authorizationService = provider.GetRequiredService<IAuthorizationService>();
    }

    [Fact]
    public async Task AstronautOnlyPolicy_Succeeds_ForAstronautRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, UserRoles.Astronaut)
        }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var result = await _authorizationService.AuthorizeAsync(user, AuthPolicies.AstronautOnly);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AstronautOnlyPolicy_Fails_ForMonitorRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-2"),
            new Claim(ClaimTypes.Role, UserRoles.Monitor)
        }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var result = await _authorizationService.AuthorizeAsync(user, AuthPolicies.AstronautOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task MonitorOnlyPolicy_Succeeds_ForMonitorRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-3"),
            new Claim(ClaimTypes.Role, UserRoles.Monitor)
        }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var result = await _authorizationService.AuthorizeAsync(user, AuthPolicies.MonitorOnly);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task MonitorOnlyPolicy_Fails_ForAstronautRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-4"),
            new Claim(ClaimTypes.Role, UserRoles.Astronaut)
        }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var result = await _authorizationService.AuthorizeAsync(user, AuthPolicies.MonitorOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthenticatedPolicy_Fails_ForAnonymousUser()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _authorizationService.AuthorizeAsync(anonymousUser, AuthPolicies.Authenticated);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthenticatedPolicy_Succeeds_ForAnyAuthenticatedRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-5")
        }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var result = await _authorizationService.AuthorizeAsync(user, AuthPolicies.Authenticated);

        Assert.True(result.Succeeded);
    }
}

