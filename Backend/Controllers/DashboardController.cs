using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Dashboard;
using AstronautHealthMonitoring.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IDashboardService dashboardService,
        ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Generates a comprehensive ground-monitor overview of crew vitals, active alerts, and missions (MonitorOnly).
    /// </summary>
    [HttpGet("monitor")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(MonitorDashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMonitorDashboard(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetMonitorDashboardAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a full health and operational telemetry snapshot for a specific astronaut (MonitorOnly).
    /// </summary>
    [HttpGet("astronauts/{id:int}")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(AstronautDashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAstronautSnapshot(int id, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetAstronautSnapshotAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Astronaut with ID {id} not found." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Retrieves the personal health and operational dashboard for the authenticated astronaut (AstronautOnly).
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.AstronautOnly)]
    [ProducesResponseType(typeof(AstronautDashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyDashboard(CancellationToken cancellationToken)
    {
        var astronautId = _currentUserService.AstronautId;
        if (astronautId == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
        }

        try
        {
            var result = await _dashboardService.GetMyDashboardAsync(astronautId.Value, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}

