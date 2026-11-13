using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.Mood;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoodController : ControllerBase
{
    private readonly IMoodService _moodService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateMoodEntryRequest> _createValidator;

    public MoodController(
        IMoodService moodService,
        ICurrentUserService currentUserService,
        IValidator<CreateMoodEntryRequest> createValidator)
    {
        _moodService = moodService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Records a psychological check-in entry for the authenticated astronaut.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.AstronautOnly)]
    [ProducesResponseType(typeof(MoodEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordMood([FromBody] CreateMoodEntryRequest request, CancellationToken cancellationToken)
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
            var result = await _moodService.RecordMoodAsync(astronautId.Value, request, cancellationToken);
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
    /// Retrieves a single mood entry by its ID. Astronauts can only access their own mood entries.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(MoodEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _moodService.GetMoodEntryByIdAsync(id, cancellationToken);
            if (entry == null)
            {
                return NotFound(new { message = $"Mood entry with ID {id} not found." });
            }

            return Ok(entry);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves the most recent psychological check-in entry for an astronaut.
    /// </summary>
    [HttpGet("latest")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(MoodEntryDto), StatusCodes.Status200OK)]
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
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own mood entries." });
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
            var entry = await _moodService.GetLatestMoodEntryAsync(targetAstronautId, cancellationToken);
            if (entry == null)
            {
                return NotFound(new { message = $"No mood entries found for astronaut {targetAstronautId}." });
            }

            return Ok(entry);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves paginated mood check-in history with optional date range filters.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(PagedResult<MoodEntryDto>), StatusCodes.Status200OK)]
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
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own mood entries." });
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
            var results = await _moodService.GetMoodHistoryPagedAsync(targetAstronautId, fromUtc, toUtc, pageNumber, pageSize, cancellationToken);
            return Ok(results);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}

