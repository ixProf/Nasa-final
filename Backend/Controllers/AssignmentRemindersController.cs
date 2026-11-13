using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Services.Implementations;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AstronautHealthMonitoring.Controllers;

[ApiController, Route("api/reminders"), Authorize(Policy = AuthPolicies.Authenticated)]
public class AssignmentRemindersController(AssignmentReminderService reminders, ICurrentUserService user) : ControllerBase
{
    private bool IsMonitor => user.IsInRole(UserRoles.Monitor);
    private int? Owner => IsMonitor ? null : user.AstronautId;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? astronautId, CancellationToken ct)
    {
        if (!IsMonitor)
        {
            if (!user.IsInRole(UserRoles.Astronaut) || user.AstronautId == null || astronautId.HasValue && astronautId != user.AstronautId) return Forbid();
            astronautId = user.AstronautId;
        }
        if (astronautId is not > 0) return BadRequest(new { message = "Select an astronaut." });
        return Ok(await reminders.GetAsync(astronautId.Value, ct));
    }

    [HttpPut("{id:int}/settings")]
    public Task<IActionResult> Update(int id, UpdateAssignmentReminderRequest request, CancellationToken ct) => Handle(async () =>
    {
        if (!IsMonitor && (!user.IsInRole(UserRoles.Astronaut) || Owner == null)) return Forbid();
        await reminders.UpdateAsync(id, Owner, request, ct); return NoContent();
    });

    [HttpPost("{id:int}/send"), Authorize(Policy = AuthPolicies.MonitorOnly)]
    public Task<IActionResult> Send(int id, CancellationToken ct) => Handle(async () => Ok(await reminders.SendAsync(id, true, user.FullName, ct)));

    [HttpPost("{id:int}/acknowledge"), Authorize(Policy = AuthPolicies.AstronautOnly)]
    public Task<IActionResult> Acknowledge(int id, [FromBody] AcknowledgeReminderRequest request, CancellationToken ct) => Handle(async () =>
    {
        if (Owner is not int owner) return Forbid();
        await reminders.AcknowledgeAsync(id, owner, request.Token, ct); return NoContent();
    });

    private async Task<IActionResult> Handle(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "Reminder changed. Refresh and retry." }); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return Conflict(new { message = "Reminder changed. Refresh and retry." }); }
    }
    public record AcknowledgeReminderRequest(Guid Token);
}
