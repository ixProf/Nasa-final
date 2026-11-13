using AstronautHealthMonitoring.Controllers;
using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.Mapping;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Validators.Chat;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AstronautHealthMonitoring.Tests;

public class ChatControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ChatService _service;
    private readonly TestCurrentUserService _currentUserService;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChatControllerDb_{Guid.NewGuid()}")
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

        _service = new ChatService(
            _context,
            _mapper,
            NullLogger<ChatService>.Instance);

        _currentUserService = new TestCurrentUserService
        {
            Role = UserRoles.Astronaut,
            UserId = "astro-01",
            AstronautId = 1,
            FullName = "Major Tom",
            IsAuthenticated = true
        };

        _controller = new ChatController(
            _service,
            _currentUserService,
            new SendChatMessageRequestValidator());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetMessages_AstronautViewingOwnThread_ReturnsOk()
    {
        var result = await _controller.GetMessages(astronautId: 1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var messages = Assert.IsAssignableFrom<IReadOnlyList<ChatMessageDto>>(okResult.Value);
        Assert.NotNull(messages);
    }

    [Fact]
    public async Task GetMessages_AstronautViewingOtherThread_ReturnsForbidden()
    {
        var result = await _controller.GetMessages(astronautId: 2);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetMessages_MonitorSelectingAstronaut_ReturnsOk()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var result = await _controller.GetMessages(astronautId: 1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var messages = Assert.IsAssignableFrom<IReadOnlyList<ChatMessageDto>>(okResult.Value);
        Assert.NotNull(messages);
    }

    [Fact]
    public async Task GetMessages_MonitorMissingAstronautId_ReturnsBadRequest()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var result = await _controller.GetMessages(astronautId: null);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task SendMessage_AstronautSendingOwnMessage_ReturnsCreated()
    {
        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Checking in from cupola.",
            RecordedAt = DateTime.UtcNow
        };

        var result = await _controller.SendMessage(request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var dto = Assert.IsType<ChatMessageDto>(created.Value);
        Assert.Equal(1, dto.AstronautId);
    }

    [Fact]
    public async Task SendMessage_AstronautSendingToOther_ReturnsForbidden()
    {
        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            AstronautId = 2, // Calling user is Astronaut 1
            Content = "Cross thread attempt",
            RecordedAt = DateTime.UtcNow
        };

        var result = await _controller.SendMessage(request);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task SendMessage_MonitorSendingMessage_ReturnsCreated()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            AstronautId = 2,
            Content = "Flight surgeon checking your schedule.",
            RecordedAt = DateTime.UtcNow
        };

        var result = await _controller.SendMessage(request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var dto = Assert.IsType<ChatMessageDto>(created.Value);
        Assert.Equal(2, dto.AstronautId);
    }

    [Fact]
    public async Task SendMessage_DeactivatedAstronaut_ReturnsConflict()
    {
        var astro = await _context.Astronauts.FirstAsync(a => a.Id == 1);
        astro.IsActive = false;
        await _context.SaveChangesAsync();

        var request = new SendChatMessageRequest
        {
            ClientGeneratedId = Guid.NewGuid(),
            Content = "Hello?",
            RecordedAt = DateTime.UtcNow
        };

        var result = await _controller.SendMessage(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task MarkMessageAsRead_ReturnsOk()
    {
        var msg = new ChatMessage
        {
            ClientGeneratedId = Guid.NewGuid(),
            AstronautId = 1,
            SenderUserId = "monitor-01",
            SenderRole = UserRoles.Monitor,
            Content = "Unread message",
            RecordedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync();

        var result = await _controller.MarkMessageAsRead(msg.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ChatMessageDto>(okResult.Value);
        Assert.True(dto.IsRead);
    }

    [Fact]
    public async Task GetUnreadCount_AstronautViewingOwn_ReturnsOk()
    {
        var result = await _controller.GetUnreadCount(astronautId: null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetUnreadCount_MonitorMissingAstronautId_ReturnsBadRequest()
    {
        _currentUserService.Role = UserRoles.Monitor;
        _currentUserService.AstronautId = null;

        var result = await _controller.GetUnreadCount(astronautId: null);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }
}

