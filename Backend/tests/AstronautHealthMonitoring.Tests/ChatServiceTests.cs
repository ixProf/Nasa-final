using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class TestChatNotifier : IChatNotifier
{
    public List<ChatMessageDto> ReceivedMessages { get; } = new();
    public List<ChatMessageDto> ReadMessages { get; } = new();

    public Task NotifyMessageReceivedAsync(ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        ReceivedMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task NotifyMessageReadAsync(ChatMessageDto message, CancellationToken cancellationToken = default)
    {
        ReadMessages.Add(message);
        return Task.CompletedTask;
    }
}

public class ChatServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly TestChatNotifier _chatNotifier;
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChatServiceDb_{Guid.NewGuid()}")
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

        _chatNotifier = new TestChatNotifier();

        _service = new ChatService(
            _context,
            _mapper,
            NullLogger<ChatService>.Instance,
            _chatNotifier);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task SendMessageAsync_ValidMessage_PersistsAndNotifies()
    {
        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Ground control, reading nominal.",
            RecordedAt = DateTime.UtcNow
        };

        var result = await _service.SendMessageAsync(1, request, "astro-user-01", UserRoles.Astronaut);

        Assert.NotNull(result);
        Assert.Equal(1, result.AstronautId);
        Assert.Equal("Major Tom", result.AstronautFullName);
        Assert.Equal("Ground control, reading nominal.", result.Content);
        Assert.Equal("astro-user-01", result.SenderUserId);
        Assert.Equal(UserRoles.Astronaut, result.SenderRole);
        Assert.False(result.IsRead);

        Assert.Single(_chatNotifier.ReceivedMessages);
        Assert.Equal(result.Id, _chatNotifier.ReceivedMessages[0].Id);
    }

    [Fact]
    public async Task SendMessageAsync_IdempotentOnClientGeneratedId_ReturnsExistingWithoutDuplicate()
    {
        var clientMsgId = Guid.NewGuid();
        var request1 = new SendChatMessageRequest
        {
            ClientGeneratedId = clientMsgId,
            Content = "First attempt",
            RecordedAt = DateTime.UtcNow
        };

        var first = await _service.SendMessageAsync(1, request1, "astro-user-01", UserRoles.Astronaut);

        var request2 = new SendChatMessageRequest
        {
            ClientGeneratedId = clientMsgId,
            Content = "Resend of same message",
            RecordedAt = DateTime.UtcNow
        };

        var second = await _service.SendMessageAsync(1, request2, "astro-user-01", UserRoles.Astronaut);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("First attempt", second.Content);

        var count = await _context.ChatMessages.CountAsync(c => c.ClientGeneratedId == clientMsgId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SendMessageAsync_DeactivatedAstronaut_ThrowsInvalidOperationException()
    {
        var astronaut = await _context.Astronauts.FirstAsync(a => a.Id == 1);
        astronaut.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Hello?",
            RecordedAt = DateTime.UtcNow
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendMessageAsync(1, request, "astro-user-01", UserRoles.Astronaut));

        Assert.Contains("deactivated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyOrWhitespaceContent_ThrowsArgumentException()
    {
        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "   ",
            RecordedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SendMessageAsync(1, request, "astro-user-01", UserRoles.Astronaut));
    }

    [Fact]
    public async Task SendMessageAsync_NonExistentAstronaut_ThrowsKeyNotFoundException()
    {
        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Message",
            RecordedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.SendMessageAsync(9999, request, "astro-user-01", UserRoles.Astronaut));
    }

    [Fact]
    public async Task GetMessagesPagedAsync_ReturnsPagedMessages()
    {
        for (int i = 1; i <= 5; i++)
        {
            await _service.SendMessageAsync(1, new SendChatMessageRequest
            {
                ClientGeneratedId = Guid.NewGuid(),
                Content = $"Message {i}",
                RecordedAt = DateTime.UtcNow
            }, "astro-01", UserRoles.Astronaut);
        }

        var page1 = await _service.GetMessagesPagedAsync(1, pageNumber: 1, pageSize: 3);
        Assert.Equal(3, page1.Count);

        var page2 = await _service.GetMessagesPagedAsync(1, pageNumber: 2, pageSize: 3);
        Assert.Equal(2, page2.Count);
    }

    [Fact]
    public async Task MarkAsReadAsync_UpdatesReadAtAndNotifies()
    {
        var created = await _service.SendMessageAsync(1, new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Important instruction",
            RecordedAt = DateTime.UtcNow
        }, "monitor-01", UserRoles.Monitor);

        Assert.False(created.IsRead);

        var updated = await _service.MarkAsReadAsync(created.Id, "astro-01", UserRoles.Astronaut);

        Assert.True(updated.IsRead);
        Assert.NotNull(updated.ReadAt);

        Assert.Single(_chatNotifier.ReadMessages);
        Assert.Equal(created.Id, _chatNotifier.ReadMessages[0].Id);
    }

    [Fact]
    public async Task GetUnreadCountAsync_AstronautRole_CountsOnlyMonitorMessages()
    {
        // 1 message sent by Astronaut (unread)
        await _service.SendMessageAsync(1, new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Astro says hello",
            RecordedAt = DateTime.UtcNow
        }, "astro-01", UserRoles.Astronaut);

        // 2 messages sent by Monitor (unread)
        await _service.SendMessageAsync(1, new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Monitor note 1",
            RecordedAt = DateTime.UtcNow
        }, "monitor-01", UserRoles.Monitor);

        await _service.SendMessageAsync(1, new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Monitor note 2",
            RecordedAt = DateTime.UtcNow
        }, "monitor-01", UserRoles.Monitor);

        // For astronaut: unread count should be 2 (only monitor messages)
        var astroUnread = await _service.GetUnreadCountAsync(1, UserRoles.Astronaut);
        Assert.Equal(2, astroUnread);

        // For monitor: unread count should be 1 (only astronaut messages)
        var monitorUnread = await _service.GetUnreadCountAsync(1, UserRoles.Monitor);
        Assert.Equal(1, monitorUnread);
    }
}

