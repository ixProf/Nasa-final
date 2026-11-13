using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AstronautHealthMonitoring.Services.Implementations;

public class AssignmentReminderService(AppDbContext db, TimeProvider clock, IAssignmentReminderNotifier notifier)
{
    public static AssignmentReminderDeliveryDto ToDto(AssignmentReminderDelivery d) => new(
        d.RecommendationId, d.Recommendation.AstronautId, d.Token, d.Recommendation.Title, d.Recommendation.TitleAr,
        d.Recommendation.Content, d.Recommendation.ContentAr, d.SentAt, d.IsManual, d.SoundEnabled, d.SenderName);

    private async Task<Recommendation> AssignmentAsync(int id, int? owner, CancellationToken ct)
    {
        var assignment = await db.Recommendations.Include(r => r.Astronaut).SingleOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("Assignment not found.");
        if (owner.HasValue && assignment.AstronautId != owner) throw new UnauthorizedAccessException("You can only manage your own reminders.");
        return assignment;
    }

    private async Task<bool> CompletedTodayAsync(Recommendation r, DateTime now, CancellationToken ct) => r.IsDailyHabit &&
        await db.HabitCompletions.AnyAsync(c => c.RecommendationId == r.Id && c.CompletedOn == DateOnly.FromDateTime(now), ct);

    private static bool Active(Recommendation r) => r.Astronaut.IsActive && r.Status != RecommendationStatus.Dismissed && (r.IsDailyHabit || r.Status != RecommendationStatus.Completed);

    public async Task<AssignmentRemindersDto> GetAsync(int astronautId, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var assignments = await db.Recommendations.Include(r => r.Astronaut).AsNoTracking().Where(r => r.AstronautId == astronautId).OrderBy(r => r.Id).ToListAsync(ct);
        assignments = assignments.Where(Active).ToList();
        var ids = assignments.Select(r => r.Id).ToList();
        var settings = await db.AssignmentReminderSettings.AsNoTracking().Where(s => ids.Contains(s.RecommendationId)).ToDictionaryAsync(s => s.RecommendationId, ct);
        var completed = await db.HabitCompletions.AsNoTracking().Where(c => c.AstronautId == astronautId && c.CompletedOn == today).Select(c => c.RecommendationId).ToListAsync(ct);
        var list = assignments.Select(r => {
            settings.TryGetValue(r.Id, out var s);
            var done = r.IsDailyHabit && completed.Contains(r.Id);
            return new AssignmentReminderDto(r.Id, r.Title, r.TitleAr, r.Content, r.ContentAr, s?.Enabled ?? false,
                s?.IntervalMinutes ?? 60, s?.SoundEnabled ?? true, done ? null : s?.NextDueAt, done);
        }).ToList();
        var pending = await db.AssignmentReminderDeliveries.Include(d => d.Recommendation).AsNoTracking()
            .Where(d => ids.Contains(d.RecommendationId) && !completed.Contains(d.RecommendationId) && d.AcknowledgedAt == null)
            .OrderByDescending(d => d.SentAt).Take(20).ToListAsync(ct);
        return new(list, pending.Select(ToDto).ToList());
    }

    public async Task UpdateAsync(int id, int? owner, UpdateAssignmentReminderRequest request, CancellationToken ct = default)
    {
        if (request.IntervalMinutes is < 1 or > 10080) throw new ArgumentException("Reminder interval must be between 1 and 10080 minutes.");
        var r = await AssignmentAsync(id, owner, ct);
        if (!Active(r)) throw new InvalidOperationException("This assignment is not active.");
        var s = await db.AssignmentReminderSettings.FindAsync([id], ct);
        if (s == null) { s = new AssignmentReminderSetting { Recommendation = r }; db.AssignmentReminderSettings.Add(s); }
        s.Enabled = request.Enabled; s.IntervalMinutes = request.IntervalMinutes; s.SoundEnabled = request.SoundEnabled;
        s.NextDueAt = request.Enabled ? clock.GetUtcNow().UtcDateTime.AddMinutes(request.IntervalMinutes) : null;
        s.Revision = Guid.NewGuid();
        // Disabling a reminder also clears its outstanding automatic popup.
        if (!request.Enabled)
        {
            var delivery = await db.AssignmentReminderDeliveries.FindAsync([id], ct);
            if (delivery != null && !delivery.IsManual) delivery.AcknowledgedAt = clock.GetUtcNow().UtcDateTime;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task AcknowledgeAsync(int id, int owner, Guid token, CancellationToken ct = default)
    {
        await AssignmentAsync(id, owner, ct);
        var d = await db.AssignmentReminderDeliveries.SingleOrDefaultAsync(d => d.RecommendationId == id && d.Token == token, ct);
        if (d != null)
        {
            d.AcknowledgedAt = clock.GetUtcNow().UtcDateTime;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { db.Entry(d).State = EntityState.Detached; /* A newer reminder must remain unread. */ }
        }
    }

    public async Task<AssignmentReminderDeliveryDto?> SendAsync(int id, bool manual, string? sender = null, CancellationToken ct = default)
    {
        var r = await AssignmentAsync(id, null, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        if (!Active(r) || await CompletedTodayAsync(r, now, ct))
        {
            if (manual) throw new InvalidOperationException("This assignment is already complete or inactive.");
            return null;
        }
        var setting = await db.AssignmentReminderSettings.FindAsync([id], ct);
        if (setting == null)
        {
            if (!manual) return null;
            setting = new AssignmentReminderSetting { Recommendation = r, Enabled = false };
            db.AssignmentReminderSettings.Add(setting);
        }
        if (!manual && (!setting.Enabled || setting.NextDueAt == null || setting.NextDueAt > now)) return null;
        var delivery = await db.AssignmentReminderDeliveries.SingleOrDefaultAsync(d => d.RecommendationId == id, ct);
        if (manual && delivery?.IsManual == true && delivery.SentAt > now.AddSeconds(-5)) return ToDto(delivery);
        if (delivery == null) { delivery = new AssignmentReminderDelivery { Recommendation = r }; db.AssignmentReminderDeliveries.Add(delivery); }
        delivery.Token = Guid.NewGuid(); delivery.SentAt = now; delivery.AcknowledgedAt = null;
        delivery.SoundEnabled = setting.SoundEnabled; delivery.IsManual = manual; delivery.SenderName = sender;
        if (db.Entry(delivery).State != EntityState.Added) db.Entry(delivery).Property(d => d.AcknowledgedAt).IsModified = true;
        setting.Revision = Guid.NewGuid();
        if (setting.Enabled) setting.NextDueAt = now.AddMinutes(setting.IntervalMinutes);
        await db.SaveChangesAsync(ct);
        var dto = ToDto(delivery);
        await notifier.NotifyAsync(dto, ct);
        return dto;
    }

    public async Task DispatchDueAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var ids = await db.AssignmentReminderSettings.AsNoTracking().Where(s => s.Enabled && s.NextDueAt <= now && s.Recommendation.Astronaut.IsActive
            && s.Recommendation.Status != RecommendationStatus.Dismissed
            && (s.Recommendation.IsDailyHabit || s.Recommendation.Status != RecommendationStatus.Completed)
            && !db.HabitCompletions.Any(c => c.RecommendationId == s.RecommendationId && c.CompletedOn == today))
            .OrderBy(s => s.NextDueAt).Select(s => s.RecommendationId).Take(50).ToListAsync(ct);
        foreach (var id in ids)
        {
            db.ChangeTracker.Clear();
            try { await SendAsync(id, false, ct: ct); }
            catch (DbUpdateConcurrencyException) { /* A different worker or preference update won this reminder. */ }
            catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { /* A different worker created the first inbox entry. */ }
        }
    }
}
