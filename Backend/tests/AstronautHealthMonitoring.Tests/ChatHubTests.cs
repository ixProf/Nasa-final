using System.Reflection;
using System.Security.Claims;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.Hubs;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class ChatHubTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ChatService _chatService;

    public ChatHubTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChatHubDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _context.Astronauts.AddRange(
            new Astronaut
            {
                Id = 1,
                AstronautCode = "AST-01",
                FirstName = "Major",
                LastName = "Tom",
                FullName = "Major Tom",
                DateOfBirth = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                BloodType = "O+",
                HeightCm = 175,
                WeightKg = 70,
                IsActive = true
            },
            new Astronaut
            {
                Id = 2,
                AstronautCode = "AST-02",
                FirstName = "Sarah",
                LastName = "Connor",
                FullName = "Sarah Connor",
                DateOfBirth = new DateTime(1985, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Female",
                BloodType = "A+",
                HeightCm = 168,
                WeightKg = 62,
                IsActive = true
            }
        );
        _context.SaveChanges();

        var config = new TypeAdapterConfig();
        new ChatMappings().Register(config);
        _mapper = new Mapper(config);

        _chatService = new ChatService(
            _context,
            _mapper,
            NullLogger<ChatService>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void ChatHub_Has_AuthorizeAttribute()
    {
        var authorizeAttr = typeof(ChatHub).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
    }

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

        var hub = new ChatHub(_chatService, NullLogger<ChatHub>.Instance)
        {
            Context = context,
            Groups = groupManager
        };

        await hub.OnConnectedAsync();

        Assert.Contains(groupManager.AddedGroups, g =>
            g.ConnectionId == "conn-monitor-1" && g.GroupName == ChatHub.MonitorsGroupName);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenAstronautConnects_AddsToAstronautGroup()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "astro-01"),
            new Claim(ClaimTypes.Role, UserRoles.Astronaut),
            new Claim("astronaut_id", "1")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var groupManager = new TestGroupManager();
        var context = new TestHubCallerContext("conn-astro-1", principal);

        var hub = new ChatHub(_chatService, NullLogger<ChatHub>.Instance)
        {
            Context = context,
            Groups = groupManager
        };

        await hub.OnConnectedAsync();

        Assert.Contains(groupManager.AddedGroups, g =>
            g.ConnectionId == "conn-astro-1" && g.GroupName == ChatHub.GetAstronautGroupName(1));
    }

    [Fact]
    public async Task JoinThread_WhenAstronautJoinsOwnThread_Succeeds()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "astro-01"),
            new Claim(ClaimTypes.Role, UserRoles.Astronaut),
            new Claim("astronaut_id", "1")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var groupManager = new TestGroupManager();
        var context = new TestHubCallerContext("conn-astro-1", principal);

        var hub = new ChatHub(_chatService, NullLogger<ChatHub>.Instance)
        {
            Context = context,
            Groups = groupManager
        };

        await hub.JoinThread(1);

        Assert.Contains(groupManager.AddedGroups, g =>
            g.ConnectionId == "conn-astro-1" && g.GroupName == ChatHub.GetAstronautGroupName(1));
    }

    [Fact]
    public async Task JoinThread_WhenAstronautJoinsOtherThread_ThrowsHubException()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "astro-01"),
            new Claim(ClaimTypes.Role, UserRoles.Astronaut),
            new Claim("astronaut_id", "1")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var groupManager = new TestGroupManager();
        var context = new TestHubCallerContext("conn-astro-1", principal);

        var hub = new ChatHub(_chatService, NullLogger<ChatHub>.Instance)
        {
            Context = context,
            Groups = groupManager
        };

        var ex = await Assert.ThrowsAsync<HubException>(() => hub.JoinThread(2));
        Assert.Contains("Astronauts cannot join threads of other astronauts", ex.Message);
    }

    [Fact]
    public async Task JoinThread_WhenMonitorJoinsAnyThread_Succeeds()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "monitor-01"),
            new Claim(ClaimTypes.Role, UserRoles.Monitor)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var groupManager = new TestGroupManager();
        var context = new TestHubCallerContext("conn-mon-1", principal);

        var hub = new ChatHub(_chatService, NullLogger<ChatHub>.Instance)
        {
            Context = context,
            Groups = groupManager
        };

        await hub.JoinThread(2);

        Assert.Contains(groupManager.AddedGroups, g =>
            g.ConnectionId == "conn-mon-1" && g.GroupName == ChatHub.GetAstronautGroupName(2));
    }

    [Fact]
    public async Task SendMessage_WhenCalledByAstronaut_ResolvesAstronautId()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "astro-01"),
            new Claim(ClaimTypes.Role, UserRoles.Astronaut),
            new Claim("astronaut_id", "1")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var context = new TestHubCallerContext("conn-astro-1", principal);

        var hub = new ChatHub(_chatService, NullLogger<ChatHub>.Instance)
        {
            Context = context
        };

        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "SignalR test message",
            RecordedAt = DateTime.UtcNow
        };

        var result = await hub.SendMessage(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.AstronautId);
        Assert.Equal("SignalR test message", result.Content);
    }

    [Fact]
    public async Task SendMessage_WhenCalledByMonitor_UsesTargetAstronautId()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "monitor-01"),
            new Claim(ClaimTypes.Role, UserRoles.Monitor)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var context = new TestHubCallerContext("conn-mon-1", principal);

        var hub = new ChatHub(_chatService, NullLogger<ChatHub>.Instance)
        {
            Context = context
        };

        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Monitor message via SignalR",
            RecordedAt = DateTime.UtcNow
        };

        var result = await hub.SendMessage(request, targetAstronautId: 2);

        Assert.NotNull(result);
        Assert.Equal(2, result.AstronautId);
    }

    [Fact]
    public async Task SendMessage_WhenMonitorMissingTarget_ThrowsHubException()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "monitor-01"),
            new Claim(ClaimTypes.Role, UserRoles.Monitor)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var context = new TestHubCallerContext("conn-mon-1", principal);

        var hub = new ChatHub(_chatService, NullLogger<ChatHub>.Instance)
        {
            Context = context
        };

        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Missing target message",
            RecordedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<HubException>(() => hub.SendMessage(request));
    }
}

