using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class ChatService : IChatService
{
    private readonly AppDbContext _dbContext;
    private readonly IChatNotifier? _chatNotifier;
    private readonly IMapper _mapper;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        AppDbContext dbContext,
        IMapper mapper,
        ILogger<ChatService> logger,
        IChatNotifier? chatNotifier = null)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _chatNotifier = chatNotifier;
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        int astronautId,
        SendChatMessageRequest request,
        string senderUserId,
        string senderRole,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (astronautId <= 0)
        {
            throw new ArgumentException("A valid positive AstronautId must be provided.", nameof(astronautId));
        }

        var astronaut = await _dbContext.Astronauts.FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);
        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} does not exist.");
        }

        if (!astronaut.IsActive)
        {
            throw new InvalidOperationException($"Astronaut '{astronaut.AstronautCode}' is deactivated and cannot send or receive chat messages.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Message content cannot be empty or whitespace.", nameof(request));
        }

        if (request.Content.Length > 4000)
        {
            throw new ArgumentException("Message content cannot exceed 4000 characters.", nameof(request));
        }

        // Idempotency check: if ClientGeneratedId is non-empty, check if already recorded
        if (request.ClientGeneratedId != Guid.Empty)
        {
            var existingMessage = await _dbContext.ChatMessages
                .Include(c => c.Astronaut)
                .FirstOrDefaultAsync(c => c.ClientGeneratedId == request.ClientGeneratedId, cancellationToken);

            if (existingMessage != null)
            {
                _logger.LogInformation("Idempotent hit: ChatMessage with ClientGeneratedId {ClientGeneratedId} already exists.", request.ClientGeneratedId);
                return _mapper.Map<ChatMessageDto>(existingMessage);
            }
        }
        else
        {
            request.ClientGeneratedId = Guid.NewGuid();
        }

        var message = new ChatMessage
        {
            ClientGeneratedId = request.ClientGeneratedId,
            AstronautId = astronautId,
            SenderUserId = senderUserId,
            SenderRole = senderRole,
            Content = request.Content.Trim(),
            RecordedAt = request.RecordedAt != default ? request.RecordedAt : DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(message).Reference(c => c.Astronaut).LoadAsync(cancellationToken);

        var dto = _mapper.Map<ChatMessageDto>(message);

        if (_chatNotifier != null)
        {
            await _chatNotifier.NotifyMessageReceivedAsync(dto, cancellationToken);
        }

        return dto;
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetThreadHistoryAsync(
        int astronautId,
        int take = 50,
        DateTime? beforeUtc = null,
        CancellationToken cancellationToken = default)
    {
        var astronautExists = await _dbContext.Astronauts.AnyAsync(a => a.Id == astronautId, cancellationToken);
        if (!astronautExists)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} does not exist.");
        }

        var query = _dbContext.ChatMessages
            .AsNoTracking()
            .Where(c => c.AstronautId == astronautId);

        if (beforeUtc.HasValue)
        {
            query = query.Where(c => c.SentAt < beforeUtc.Value);
        }

        var messages = await query
            .OrderByDescending(c => c.SentAt)
            .Take(take)
            .Include(c => c.Astronaut)
            .ToListAsync(cancellationToken);

        messages.Reverse();

        return _mapper.Map<List<ChatMessageDto>>(messages);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesPagedAsync(
        int astronautId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var astronautExists = await _dbContext.Astronauts.AnyAsync(a => a.Id == astronautId, cancellationToken);
        if (!astronautExists)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} does not exist.");
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var messages = await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(c => c.AstronautId == astronautId)
            .OrderBy(c => c.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Astronaut)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<ChatMessageDto>>(messages);
    }

    public async Task<ChatMessageDto> MarkAsReadAsync(
        int messageId,
        string readerUserId,
        string readerRole,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.ChatMessages
            .Include(c => c.Astronaut)
            .FirstOrDefaultAsync(c => c.Id == messageId, cancellationToken);

        if (message == null)
        {
            throw new KeyNotFoundException($"ChatMessage with ID {messageId} not found.");
        }

        if (!message.ReadAt.HasValue)
        {
            message.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var dto = _mapper.Map<ChatMessageDto>(message);

        if (_chatNotifier != null)
        {
            await _chatNotifier.NotifyMessageReadAsync(dto, cancellationToken);
        }

        return dto;
    }

    public async Task<bool> MarkThreadAsReadAsync(
        int astronautId,
        string readerUserId,
        CancellationToken cancellationToken = default)
    {
        var unreadMessages = await _dbContext.ChatMessages
            .Where(c => c.AstronautId == astronautId && c.ReadAt == null)
            .ToListAsync(cancellationToken);

        if (unreadMessages.Count == 0)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        foreach (var msg in unreadMessages)
        {
            msg.ReadAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetUnreadCountAsync(
        int astronautId,
        string readerRole,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ChatMessages
            .AsNoTracking()
            .Where(c => c.AstronautId == astronautId && c.ReadAt == null);

        if (string.Equals(readerRole, UserRoles.Astronaut, StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.SenderRole != UserRoles.Astronaut);
        }
        else if (string.Equals(readerRole, UserRoles.Monitor, StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.SenderRole == UserRoles.Astronaut);
        }

        return await query.CountAsync(cancellationToken);
    }
}

