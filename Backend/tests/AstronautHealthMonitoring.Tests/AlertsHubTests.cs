using System.Security.Claims;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Hubs;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class TestGroupManager : IGroupManager
{
    public List<(string ConnectionId, string GroupName)> AddedGroups { get; } = new();

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        AddedGroups.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class TestHubCallerContext : HubCallerContext
{
    public override string ConnectionId { get; }
    public override string? UserIdentifier { get; }
    public override ClaimsPrincipal? User { get; }
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;

    public TestHubCallerContext(string connectionId, ClaimsPrincipal? user)
    {
        ConnectionId = connectionId;
        User = user;
        UserIdentifier = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public override void Abort() { }
}

public class AlertsHubTests
{
    [Fact]
    public async Task OnConnectedAsync_WhenMonitorConnects_AddsToMonitorsGroup()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "monitor-01"),
            new Claim(ClaimTypes.Role, UserRoles.Monitor)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var groupManager = new TestGroupManager();
        var context = new TestHubCallerContext("conn-monitor-1", principal);

        var currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Monitor,
            AstronautId = null,
            UserId = "monitor-01"
        };

        var hub = new AlertsHub(NullLogger<AlertsHub>.Instance, currentUserService)
        {
            Context = context,
            Groups = groupManager
        };

        await hub.OnConnectedAsync();

        Assert.Single(groupManager.AddedGroups);
        Assert.Equal("conn-monitor-1", groupManager.AddedGroups[0].ConnectionId);
        Assert.Equal("monitors", groupManager.AddedGroups[0].GroupName);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenAstronautConnects_AddsToAstronautGroup()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "astro-01"),
            new Claim(ClaimTypes.Role, UserRoles.Astronaut),
            new Claim("astronaut_id", "42")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var groupManager = new TestGroupManager();
        var context = new TestHubCallerContext("conn-astro-42", principal);

        var currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Astronaut,
            AstronautId = 42,
            UserId = "astro-01"
        };

        var hub = new AlertsHub(NullLogger<AlertsHub>.Instance, currentUserService)
        {
            Context = context,
            Groups = groupManager
        };

        await hub.OnConnectedAsync();

        Assert.Single(groupManager.AddedGroups);
        Assert.Equal("conn-astro-42", groupManager.AddedGroups[0].ConnectionId);
        Assert.Equal("astronaut-42", groupManager.AddedGroups[0].GroupName);
    }
}

