using AstronautHealthMonitoring.DTOs.Alerts;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.Models.Enums;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/alerts")]
public class HealthAlertsController : ControllerBase
{
    private readonly IHealthAlertService _healthAlertService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<TriggerManualAlertRequest> _manualAlertValidator;
    private readonly IValidator<AcknowledgeAlertRequest> _acknowledgeValidator;
    private readonly IValidator<ResolveAlertRequest> _resolveValidator;

    public HealthAlertsController(
        IHealthAlertService healthAlertService,
        ICurrentUserService currentUserService,
        IValidator<TriggerManualAlertRequest> manualAlertValidator,
        IValidator<AcknowledgeAlertRequest> acknowledgeValidator,
        IValidator<ResolveAlertRequest> resolveValidator)
    {
        _healthAlertService = healthAlertService;
        _currentUserService = currentUserService;
        _manualAlertValidator = manualAlertValidator;
        _acknowledgeValidator = acknowledgeValidator;
        _resolveValidator = resolveValidator;
    }

    /// <summary>
    /// Retrieves paginated safety alerts, filtered optionally by status, severity, and astronaut.
    /// Astronauts can only view their own alerts.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(PagedResult<HealthAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] AlertStatus? status,
        [FromQuery] AlertSeverity? severity,
        [FromQuery] int? astronautId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
            }

            if (astronautId.HasValue && astronautId.Value != myAstronautId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own alerts." });
            }

            astronautId = myAstronautId.Value;
        }

        try
        {
            var results = await _healthAlertService.GetAlertsPagedAsync(status, severity, astronautId, pageNumber, pageSize, cancellationToken);
            return Ok(results);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a single health alert by its ID. Astronauts can only access their own alerts.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(HealthAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var alert = await _healthAlertService.GetAlertByIdAsync(id, cancellationToken);
            if (alert == null)
            {
                return NotFound(new { message = $"Health alert with ID {id} not found." });
            }

            return Ok(alert);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Triggers a manual health safety alert from an astronaut terminal or flight monitor console.
    /// </summary>
    [HttpPost("manual")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [EnableRateLimiting("ManualAlertLimiter")]
    [ProducesResponseType(typeof(HealthAlertDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TriggerManualAlert([FromBody] TriggerManualAlertRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _manualAlertValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var alert = await _healthAlertService.TriggerManualAlertAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = alert.Id }, alert);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Acknowledges an active health safety alert and records the monitor action taken.
    /// Flight monitor role only.
    /// </summary>
    [HttpPost("{id:int}/acknowledge")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(HealthAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcknowledgeAlert(int id, [FromBody] AcknowledgeAlertRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _acknowledgeValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var monitorUserId = _currentUserService.UserId ?? "UnknownMonitor";

        try
        {
            var result = await _healthAlertService.AcknowledgeAlertAsync(id, request, monitorUserId, cancellationToken);
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

    /// <summary>
    /// Formally resolves a health safety alert with resolution notes.
    /// Flight monitor role only.
    /// </summary>
    [HttpPost("{id:int}/resolve")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(HealthAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveAlert(int id, [FromBody] ResolveAlertRequest? request, CancellationToken cancellationToken)
    {
        if (request != null)
        {
            var validationResult = await _resolveValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary());
            }
        }

        var monitorUserId = _currentUserService.UserId ?? "UnknownMonitor";

        try
        {
            var result = await _healthAlertService.ResolveAlertAsync(id, request, monitorUserId, cancellationToken);
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

