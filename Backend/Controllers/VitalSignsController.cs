using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.DTOs.VitalSigns;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VitalSignsController : ControllerBase
{
    private readonly IVitalSignService _vitalSignService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateVitalSignReadingRequest> _createValidator;

    public VitalSignsController(
        IVitalSignService vitalSignService,
        ICurrentUserService currentUserService,
        IValidator<CreateVitalSignReadingRequest> createValidator)
    {
        _vitalSignService = vitalSignService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Records a new vital sign telemetry reading for the authenticated astronaut.
    /// Evaluates reading against clinical reference ranges and triggers health alerts when non-routine.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.AstronautOnly)]
    [ProducesResponseType(typeof(VitalSignReadingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordReading([FromBody] CreateVitalSignReadingRequest request, CancellationToken cancellationToken)
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
            var result = await _vitalSignService.RecordReadingAsync(astronautId.Value, request, cancellationToken);
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
    /// Retrieves a single vital sign reading by its ID. Astronauts can only view their own readings.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(VitalSignReadingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var reading = await _vitalSignService.GetReadingByIdAsync(id, cancellationToken);
            if (reading == null)
            {
                return NotFound(new { message = $"Vital sign reading with ID {id} not found." });
            }

            return Ok(reading);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves the most recent reading for each distinct vital sign metric for an astronaut.
    /// </summary>
    [HttpGet("latest")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(IReadOnlyList<VitalSignReadingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own vital sign data." });
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
            var results = await _vitalSignService.GetLatestReadingsPerMetricAsync(targetAstronautId, cancellationToken);
            return Ok(results);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves paginated vital sign history with optional UTC date range filters.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(PagedResult<VitalSignReadingDto>), StatusCodes.Status200OK)]
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
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own vital sign data." });
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
            var results = await _vitalSignService.GetHistoryAsync(targetAstronautId, fromUtc, toUtc, pageNumber, pageSize, cancellationToken);
            return Ok(results);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all active monitored vital sign types and measurement units.
    /// </summary>
    [HttpGet("types")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(IReadOnlyList<VitalSignTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTypes(CancellationToken cancellationToken)
    {
        var types = await _vitalSignService.GetAllVitalSignTypesAsync(cancellationToken);
        return Ok(types);
    }
}

