using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Sync;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ICurrentUserService _currentUserService;

    public SyncController(
        ISyncService syncService,
        ICurrentUserService currentUserService)
    {
        _syncService = syncService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Processes an offline synchronization batch of health telemetry and chat messages.
    /// Restricted to astronauts synchronizing their personal terminal telemetry.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.AstronautOnly)]
    [ProducesResponseType(typeof(SyncBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SyncBatch(
        [FromBody] SyncBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Sync batch payload is required." });
        }

        if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only astronauts are permitted to synchronize offline device batches." });
        }

        var astronautId = _currentUserService.AstronautId;
        if (!astronautId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
        }

        try
        {
            var userId = _currentUserService.UserId ?? "unknown";
            var response = await _syncService.ProcessSyncBatchAsync(astronautId.Value, request, userId, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

