using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class RecommendationService : IRecommendationService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<RecommendationService> _logger;
    private readonly IAssignmentReminderNotifier? _reminderNotifier;
    private readonly TimeProvider _clock;

    public RecommendationService(
        AppDbContext dbContext,
        IMapper mapper,
        ILogger<RecommendationService> logger, IAssignmentReminderNotifier? reminderNotifier = null, TimeProvider? clock = null)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _reminderNotifier = reminderNotifier;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<RecommendationDto>> GetRecommendationsForAstronautAsync(
        int astronautId,
        bool pendingOnly = true,
        CancellationToken cancellationToken = default)
    {
        var astronautExists = await _dbContext.Astronauts.AnyAsync(a => a.Id == astronautId, cancellationToken);
        if (!astronautExists)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} not found.");
        }

        var query = _dbContext.Recommendations
            .Include(r => r.Astronaut)
            .AsNoTracking()
            .Where(r => r.AstronautId == astronautId);

        if (pendingOnly)
        {
            query = query.Where(r => r.Status == RecommendationStatus.Pending);
        }

        var recommendations = await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<RecommendationDto>>(recommendations);
    }

    public async Task<IReadOnlyList<RecommendationDto>> GetAllRecommendationsAsync(
        bool pendingOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Recommendations
            .Include(r => r.Astronaut)
            .AsNoTracking()
            .AsQueryable();

        if (pendingOnly)
        {
            query = query.Where(r => r.Status == RecommendationStatus.Pending);
        }

        var recommendations = await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<RecommendationDto>>(recommendations);
    }

    public async Task<RecommendationDto?> GetRecommendationByIdAsync(int recommendationId, CancellationToken cancellationToken = default)
    {
        var recommendation = await _dbContext.Recommendations
            .Include(r => r.Astronaut)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        return recommendation != null ? _mapper.Map<RecommendationDto>(recommendation) : null;
    }

    public async Task<RecommendationDto> CreateRecommendationAsync(
        int astronautId,
        CreateRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts.FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);
        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} not found.");
        }

        if (!astronaut.IsActive)
        {
            throw new InvalidOperationException($"Cannot issue recommendations for deactivated astronaut '{astronaut.AstronautCode}'.");
        }

        var recommendation = _mapper.Map<Recommendation>(request);
        recommendation.AstronautId = astronautId;
        recommendation.Astronaut = astronaut;
        recommendation.Status = RecommendationStatus.Pending;
        recommendation.GeneratedAt = DateTime.UtcNow;
        recommendation.CreatedAt = DateTime.UtcNow;

        _dbContext.Recommendations.Add(recommendation);
        var now = _clock.GetUtcNow().UtcDateTime;
        _dbContext.AssignmentReminderSettings.Add(new AssignmentReminderSetting
        {
            Recommendation = recommendation, Enabled = request.ReminderEnabled,
            IntervalMinutes = request.ReminderIntervalMinutes, SoundEnabled = request.ReminderSoundEnabled,
            NextDueAt = request.ReminderEnabled ? now.AddMinutes(request.ReminderIntervalMinutes) : null
        });
        AssignmentReminderDelivery? firstReminder = null;
        if (request.ReminderEnabled)
        {
            firstReminder = new AssignmentReminderDelivery { Recommendation = recommendation, SentAt = now, SoundEnabled = request.ReminderSoundEnabled, SenderName = request.PrescribedBy };
            _dbContext.AssignmentReminderDeliveries.Add(firstReminder);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (firstReminder != null && _reminderNotifier != null)
            await _reminderNotifier.NotifyAsync(AssignmentReminderService.ToDto(firstReminder), cancellationToken);

        _logger.LogInformation("Recommendation {Id} created for Astronaut {AstronautId}: {Title}",
            recommendation.Id, astronautId, recommendation.Title);

        return _mapper.Map<RecommendationDto>(recommendation);
    }

    public async Task<RecommendationDto> UpdateStatusAsync(
        int recommendationId,
        int astronautId,
        UpdateRecommendationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var recommendation = await _dbContext.Recommendations
            .Include(r => r.Astronaut)
            .FirstOrDefaultAsync(r => r.Id == recommendationId, cancellationToken);

        if (recommendation == null)
        {
            throw new KeyNotFoundException($"Recommendation with ID {recommendationId} not found.");
        }

        // When astronautId > 0, verify astronaut ownership
        if (astronautId > 0 && recommendation.AstronautId != astronautId)
        {
            throw new UnauthorizedAccessException("Astronauts can only update their own recommendations.");
        }

        // Enforce explicit status transition lifecycle:
        // Pending -> Acknowledged | Dismissed
        // Acknowledged -> Completed | Dismissed
        // Completed and Dismissed are terminal states.
        if (!IsValidStatusTransition(recommendation.Status, request.Status))
        {
            throw new InvalidOperationException($"Invalid recommendation status transition from '{recommendation.Status}' to '{request.Status}'.");
        }

        recommendation.Status = request.Status;
        recommendation.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recommendation {Id} status updated to {Status} for Astronaut {AstronautId}",
            recommendation.Id, request.Status, recommendation.AstronautId);

        return _mapper.Map<RecommendationDto>(recommendation);
    }

    private static bool IsValidStatusTransition(RecommendationStatus from, RecommendationStatus to)
    {
        return (from, to) switch
        {
            (RecommendationStatus.Pending, RecommendationStatus.Acknowledged) => true,
            (RecommendationStatus.Pending, RecommendationStatus.Dismissed) => true,
            (RecommendationStatus.Acknowledged, RecommendationStatus.Completed) => true,
            (RecommendationStatus.Acknowledged, RecommendationStatus.Dismissed) => true,
            _ => false
        };
    }
}

