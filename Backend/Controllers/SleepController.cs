using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.Sleep;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SleepController : ControllerBase
{
    private readonly ISleepService _sleepService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateSleepRecordRequest> _createValidator;

    public SleepController(
        ISleepService sleepService,
        ICurrentUserService currentUserService,
        IValidator<CreateSleepRecordRequest> createValidator)
    {
        _sleepService = sleepService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Records a sleep session for the authenticated astronaut.
    /// Calculates duration and verifies sleep efficiency.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.AstronautOnly)]
    [ProducesResponseType(typeof(SleepRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordSleep([FromBody] CreateSleepRecordRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var astronautId = _currentUserService.AstronautId;
        if (astronautId == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
        }

        try
        {
            var result = await _sleepService.RecordSleepAsync(astronautId.Value, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a single sleep record by its ID. Astronauts can only access their own sleep sessions.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(SleepRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var record = await _sleepService.GetSleepRecordByIdAsync(id, cancellationToken);
            if (record == null)
            {
                return NotFound(new { message = $"Sleep record with ID {id} not found." });
            }

            return Ok(record);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves the most recent sleep session for an astronaut.
    /// </summary>
    [HttpGet("latest")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(SleepRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatest([FromQuery] int? astronautId, CancellationToken cancellationToken)
    {
        int targetAstronautId;

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
            }

            if (astronautId.HasValue && astronautId.Value != myAstronautId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own sleep records." });
            }

            targetAstronautId = myAstronautId.Value;
        }
        else
        {
            if (!astronautId.HasValue || astronautId.Value <= 0)
            {
                return BadRequest(new { message = "AstronautId query parameter is required for flight monitors." });
            }

            targetAstronautId = astronautId.Value;
        }

        try
        {
            var record = await _sleepService.GetLatestSleepRecordAsync(targetAstronautId, cancellationToken);
            if (record == null)
            {
                return NotFound(new { message = $"No sleep records found for astronaut {targetAstronautId}." });
            }

            return Ok(record);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves paginated sleep session history with optional date range filters.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(PagedResult<SleepRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int? astronautId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        int targetAstronautId;

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
            }

            if (astronautId.HasValue && astronautId.Value != myAstronautId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own sleep records." });
            }

            targetAstronautId = myAstronautId.Value;
        }
        else
        {
            if (!astronautId.HasValue || astronautId.Value <= 0)
            {
                return BadRequest(new { message = "AstronautId query parameter is required for flight monitors." });
            }

            targetAstronautId = astronautId.Value;
        }

        try
        {
            var results = await _sleepService.GetSleepHistoryPagedAsync(targetAstronautId, fromUtc, toUtc, pageNumber, pageSize, cancellationToken);
            return Ok(results);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}

