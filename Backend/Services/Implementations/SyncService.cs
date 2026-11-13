using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Sync;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class SyncService : ISyncService
{
    private readonly AppDbContext _dbContext;
    private readonly IVitalSignService _vitalSignService;
    private readonly ISleepService _sleepService;
    private readonly IMoodService _moodService;
    private readonly IChatService _chatService;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        AppDbContext dbContext,
        IVitalSignService vitalSignService,
        ISleepService sleepService,
        IMoodService moodService,
        IChatService chatService,
        IConfiguration configuration,
        ILogger<SyncService> logger,
        TimeProvider? timeProvider = null)
    {
        _dbContext = dbContext;
        _vitalSignService = vitalSignService;
        _sleepService = sleepService;
        _moodService = moodService;
        _chatService = chatService;
        _configuration = configuration;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<SyncBatchResponse> ProcessSyncBatchAsync(
        int astronautId,
        SyncBatchRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var astronaut = await _dbContext.Astronauts.FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);
        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} does not exist.");
        }

        if (!astronaut.IsActive)
        {
            throw new InvalidOperationException($"Astronaut '{astronaut.AstronautCode}' is deactivated and cannot sync telemetry.");
        }

        var totalItems = request.VitalSignReadings.Count +
                         request.SleepRecords.Count +
                         request.MoodEntries.Count +
                         request.ChatMessages.Count;

        var maxBatchSize = _configuration.GetValue("Sync:MaxBatchSize", 200);
        if (totalItems > maxBatchSize)
        {
            throw new ArgumentException($"Batch size of {totalItems} items exceeds maximum allowed size of {maxBatchSize} items.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var toleranceMinutes = _configuration.GetValue("Sync:FutureToleranceMinutes", 5.0);
        var maxAgeDays = _configuration.GetValue("Sync:MaxAgeDays", 30.0);

        var maxFuture = now.AddMinutes(toleranceMinutes);
        var maxPast = now.AddDays(-maxAgeDays);

        var response = new SyncBatchResponse
        {
            ServerSyncedAt = now
        };

        // 1. Process Vital Signs ordered by RecordedAt
        var orderedVitals = request.VitalSignReadings.OrderBy(v => v.RecordedAt).ToList();
        foreach (var vital in orderedVitals)
        {
            if (vital.RecordedAt > maxFuture)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = vital.ClientGeneratedId,
                    ItemType = "VitalSign",
                    Status = SyncItemStatus.Rejected,
                    Reason = $"RecordedAt timestamp is in the future beyond {toleranceMinutes} minutes tolerance."
                });
                response.Errors.Add($"VitalSign {vital.ClientGeneratedId}: Timestamp in future beyond tolerance.");
                continue;
            }

            if (vital.RecordedAt < maxPast)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = vital.ClientGeneratedId,
                    ItemType = "VitalSign",
                    Status = SyncItemStatus.Rejected,
                    Reason = $"RecordedAt timestamp is older than maximum permitted window of {maxAgeDays} days."
                });
                response.Errors.Add($"VitalSign {vital.ClientGeneratedId}: Timestamp older than permitted window.");
                continue;
            }

            var existingVital = await _dbContext.VitalSignReadings
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.ClientGeneratedId == vital.ClientGeneratedId && v.AstronautId==astronautId, cancellationToken);

            if (existingVital != null)
            {
                response.DuplicatesIgnoredCount++;
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = vital.ClientGeneratedId,
                    ItemType = "VitalSign",
                    Status = SyncItemStatus.Duplicate,
                    ServerId = existingVital.Id,
                    Reason = "Duplicate item already synchronized."
                });
                continue;
            }

            try
            {
                var recorded = await _vitalSignService.RecordReadingAsync(astronautId, vital, cancellationToken);
                response.VitalSignReadingsProcessed++;
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = vital.ClientGeneratedId,
                    ItemType = "VitalSign",
                    Status = SyncItemStatus.Accepted,
                    ServerId = recorded.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record vital sign in sync batch: {Message}", ex.Message);
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = vital.ClientGeneratedId,
                    ItemType = "VitalSign",
                    Status = SyncItemStatus.Rejected,
                    Reason = ex.Message
                });
                response.Errors.Add($"VitalSign {vital.ClientGeneratedId}: {ex.Message}");
            }
        }

        // 2. Process Sleep Records ordered by RecordedAt
        var orderedSleep = request.SleepRecords.OrderBy(s => s.RecordedAt).ToList();
        foreach (var sleep in orderedSleep)
        {
            if (sleep.RecordedAt > maxFuture)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = sleep.ClientGeneratedId,
                    ItemType = "Sleep",
                    Status = SyncItemStatus.Rejected,
                    Reason = $"RecordedAt timestamp is in the future beyond {toleranceMinutes} minutes tolerance."
                });
                response.Errors.Add($"Sleep {sleep.ClientGeneratedId}: Timestamp in future beyond tolerance.");
                continue;
            }

            if (sleep.RecordedAt < maxPast)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = sleep.ClientGeneratedId,
                    ItemType = "Sleep",
                    Status = SyncItemStatus.Rejected,
                    Reason = $"RecordedAt timestamp is older than maximum permitted window of {maxAgeDays} days."
                });
                response.Errors.Add($"Sleep {sleep.ClientGeneratedId}: Timestamp older than permitted window.");
                continue;
            }

            var existingSleep = await _dbContext.SleepRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ClientGeneratedId == sleep.ClientGeneratedId && s.AstronautId==astronautId, cancellationToken);

            if (existingSleep != null)
            {
                response.DuplicatesIgnoredCount++;
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = sleep.ClientGeneratedId,
                    ItemType = "Sleep",
                    Status = SyncItemStatus.Duplicate,
                    ServerId = existingSleep.Id,
                    Reason = "Duplicate item already synchronized."
                });
                continue;
            }

            try
            {
                var recorded = await _sleepService.RecordSleepAsync(astronautId, sleep, cancellationToken);
                response.SleepRecordsProcessed++;
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = sleep.ClientGeneratedId,
                    ItemType = "Sleep",
                    Status = SyncItemStatus.Accepted,
                    ServerId = recorded.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record sleep entry in sync batch: {Message}", ex.Message);
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = sleep.ClientGeneratedId,
                    ItemType = "Sleep",
                    Status = SyncItemStatus.Rejected,
                    Reason = ex.Message
                });
                response.Errors.Add($"Sleep {sleep.ClientGeneratedId}: {ex.Message}");
            }
        }

        // 3. Process Mood Entries ordered by RecordedAt
        var orderedMood = request.MoodEntries.OrderBy(m => m.RecordedAt).ToList();
        foreach (var mood in orderedMood)
        {
            if (mood.RecordedAt > maxFuture)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = mood.ClientGeneratedId,
                    ItemType = "Mood",
                    Status = SyncItemStatus.Rejected,
                    Reason = $"RecordedAt timestamp is in the future beyond {toleranceMinutes} minutes tolerance."
                });
                response.Errors.Add($"Mood {mood.ClientGeneratedId}: Timestamp in future beyond tolerance.");
                continue;
            }

            if (mood.RecordedAt < maxPast)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = mood.ClientGeneratedId,
                    ItemType = "Mood",
                    Status = SyncItemStatus.Rejected,
                    Reason = $"RecordedAt timestamp is older than maximum permitted window of {maxAgeDays} days."
                });
                response.Errors.Add($"Mood {mood.ClientGeneratedId}: Timestamp older than permitted window.");
                continue;
            }

            var existingMood = await _dbContext.MoodEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ClientGeneratedId == mood.ClientGeneratedId && m.AstronautId==astronautId, cancellationToken);

            if (existingMood != null)
            {
                response.DuplicatesIgnoredCount++;
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = mood.ClientGeneratedId,
                    ItemType = "Mood",
                    Status = SyncItemStatus.Duplicate,
                    ServerId = existingMood.Id,
                    Reason = "Duplicate item already synchronized."
                });
                continue;
            }

            try
            {
                var recorded = await _moodService.RecordMoodAsync(astronautId, mood, cancellationToken);
                response.MoodEntriesProcessed++;
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = mood.ClientGeneratedId,
                    ItemType = "Mood",
                    Status = SyncItemStatus.Accepted,
                    ServerId = recorded.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record mood entry in sync batch: {Message}", ex.Message);
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = mood.ClientGeneratedId,
                    ItemType = "Mood",
                    Status = SyncItemStatus.Rejected,
                    Reason = ex.Message
                });
                response.Errors.Add($"Mood {mood.ClientGeneratedId}: {ex.Message}");
            }
        }

        // 4. Process Chat Messages ordered by RecordedAt
        var orderedChat = request.ChatMessages.OrderBy(c => c.RecordedAt).ToList();
        foreach (var chat in orderedChat)
        {
            if (chat.AstronautId.HasValue && chat.AstronautId.Value != 0 && chat.AstronautId.Value != astronautId)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = chat.ClientGeneratedId,
                    ItemType = "Chat",
                    Status = SyncItemStatus.Rejected,
                    Reason = "Cross-astronaut chat message rejected."
                });
                response.Errors.Add($"Chat {chat.ClientGeneratedId}: Cross-astronaut message rejected.");
                continue;
            }

            if (chat.RecordedAt > maxFuture)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = chat.ClientGeneratedId,
                    ItemType = "Chat",
                    Status = SyncItemStatus.Rejected,
                    Reason = $"RecordedAt timestamp is in the future beyond {toleranceMinutes} minutes tolerance."
                });
                response.Errors.Add($"Chat {chat.ClientGeneratedId}: Timestamp in future beyond tolerance.");
                continue;
            }

            if (chat.RecordedAt < maxPast)
            {
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = chat.ClientGeneratedId,
                    ItemType = "Chat",
                    Status = SyncItemStatus.Rejected,
                    Reason = $"RecordedAt timestamp is older than maximum permitted window of {maxAgeDays} days."
                });
                response.Errors.Add($"Chat {chat.ClientGeneratedId}: Timestamp older than permitted window.");
                continue;
            }

            var existingChat = await _dbContext.ChatMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientGeneratedId == chat.ClientGeneratedId, cancellationToken);

            if (existingChat != null)
            {
                response.DuplicatesIgnoredCount++;
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = chat.ClientGeneratedId,
                    ItemType = "Chat",
                    Status = SyncItemStatus.Duplicate,
                    ServerId = existingChat.Id,
                    Reason = "Duplicate item already synchronized."
                });
                continue;
            }

            try
            {
                var recorded = await _chatService.SendMessageAsync(astronautId, chat, userId, UserRoles.Astronaut, cancellationToken);
                response.ChatMessagesProcessed++;
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = chat.ClientGeneratedId,
                    ItemType = "Chat",
                    Status = SyncItemStatus.Accepted,
                    ServerId = recorded.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send chat message in sync batch: {Message}", ex.Message);
                response.ItemResults.Add(new SyncItemResultDto
                {
                    ClientGeneratedId = chat.ClientGeneratedId,
                    ItemType = "Chat",
                    Status = SyncItemStatus.Rejected,
                    Reason = ex.Message
                });
                response.Errors.Add($"Chat {chat.ClientGeneratedId}: {ex.Message}");
            }
        }

        response.Success = response.Errors.Count == 0;
        return response;
    }
}

