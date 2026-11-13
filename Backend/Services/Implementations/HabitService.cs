using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AstronautHealthMonitoring.Services.Implementations;

public class HabitService(AppDbContext db, TimeProvider clock)
{
    public async Task<HabitDashboardDto> GetAsync(int astronautId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var habits = await db.Recommendations.AsNoTracking().Where(r => r.AstronautId == astronautId && r.IsDailyHabit && r.Status != RecommendationStatus.Dismissed).OrderBy(r => r.Id).ToListAsync(ct);
        var completions = await db.HabitCompletions.AsNoTracking().Where(c => c.AstronautId == astronautId && c.CompletedOn <= today).ToListAsync(ct);
        var result = Summarize(completions, today);
        result.Assignments = habits.Select(r => new HabitAssignmentDto(r.Id, r.Title, r.TitleAr, r.Content, r.ContentAr,
            r.Reason, r.ReasonAr, r.HabitPoints, completions.Any(c => c.RecommendationId == r.Id && c.CompletedOn == today))).ToList();
        return result;
    }

    public async Task<HabitDashboardDto> CompleteAsync(int id, int astronautId, CancellationToken ct = default)
    {
        var habit = await db.Recommendations.Include(r => r.Astronaut).SingleOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("Habit assignment not found.");
        if (habit.AstronautId != astronautId) throw new UnauthorizedAccessException("You can only complete your own habits.");
        if (!habit.Astronaut.IsActive || !habit.IsDailyHabit || habit.Status == RecommendationStatus.Dismissed)
            throw new InvalidOperationException("This habit is not active.");
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        if (!await db.HabitCompletions.AnyAsync(c => c.RecommendationId == id && c.CompletedOn == today, ct))
        {
            var completion = new HabitCompletion { RecommendationId = id, AstronautId = astronautId, CompletedOn = today, CompletedAt = now, Points = habit.HabitPoints };
            db.HabitCompletions.Add(completion);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_HabitCompletions_RecommendationId_CompletedOn" })
            {
                // Another request completed today's habit first; never award twice.
                db.Entry(completion).State = EntityState.Detached;
            }
        }
        return await GetAsync(astronautId, ct);
    }

    public static HabitDashboardDto Summarize(IEnumerable<HabitCompletion> source, DateOnly today)
    {
        var rows = source.Where(c => c.CompletedOn <= today).ToList();
        var dates = rows.Select(c => c.CompletedOn).Distinct().Order().ToArray();
        var dateSet = dates.ToHashSet();
        var current = 0;
        var day = dateSet.Contains(today) ? today : today.AddDays(-1);
        while (dateSet.Contains(day)) { current++; day = day.AddDays(-1); }
        var longest = 0; var run = 0; DateOnly? previous = null;
        foreach (var date in dates) { run = previous?.AddDays(1) == date ? run + 1 : 1; longest = Math.Max(longest, run); previous = date; }
        var totals = rows.GroupBy(c => c.CompletedOn).ToDictionary(g => g.Key, g => new HabitDayDto(g.Key, g.Count(), g.Sum(c => c.Points)));
        var result = new HabitDashboardDto { TotalPoints = rows.Sum(c => c.Points), TotalCompletions = rows.Count, CurrentStreak = current, LongestStreak = longest };
        result.Heatmap = Enumerable.Range(0, 84).Select(i => { var date = today.AddDays(i - 83); return totals.GetValueOrDefault(date) ?? new HabitDayDto(date, 0, 0); }).ToList();
        result.Badges = new[] { (rows.Count >= 1, "first-step"), (longest >= 7, "week-streak"), (longest >= 30, "month-streak"), (result.TotalPoints >= 100, "century") }.Where(b => b.Item1).Select(b => b.Item2).ToArray();
        return result;
    }
}
