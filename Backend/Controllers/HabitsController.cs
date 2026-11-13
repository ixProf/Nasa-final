using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.Services.Interfaces;
using AstronautHealthMonitoring.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController, Route("api/habits"), Authorize(Policy = AuthPolicies.Authenticated)]
public class HabitsController(HabitService habits, ICurrentUserService user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? astronautId, CancellationToken ct)
    {
        if (user.IsInRole(UserRoles.Astronaut))
        {
            if (user.AstronautId == null || astronautId.HasValue && astronautId != user.AstronautId) return Forbid();
            astronautId = user.AstronautId;
        }
        else if (!user.IsInRole(UserRoles.Monitor)) return Forbid();
        if (!astronautId.HasValue || astronautId <= 0) return BadRequest(new { message = "Select an astronaut." });
        return Ok(await habits.GetAsync(astronautId.Value, ct));
    }

    [HttpPost("{id:int}/complete"), Authorize(Policy = AuthPolicies.AstronautOnly)]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        if (user.AstronautId is not int astronautId) return Forbid();
        try { return Ok(await habits.CompleteAsync(id, astronautId, ct)); }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
    }
}
