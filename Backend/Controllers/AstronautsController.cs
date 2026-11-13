using AstronautHealthMonitoring.DTOs.Astronaut;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AstronautsController : ControllerBase
{
    private readonly IAstronautService _astronautService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateAstronautRequest> _createValidator;
    private readonly IValidator<UpdateAstronautRequest> _updateValidator;

    public AstronautsController(
        IAstronautService astronautService,
        ICurrentUserService currentUserService,
        IValidator<CreateAstronautRequest> createValidator,
        IValidator<UpdateAstronautRequest> updateValidator)
    {
        _astronautService = astronautService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Atomically provisions an astronaut profile and associated Identity account (MonitorOnly).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(AstronautDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAstronaut([FromBody] CreateAstronautRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var result = await _astronautService.CreateAstronautAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves paginated astronaut profiles with optional mission filter (MonitorOnly).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(PagedResult<AstronautDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAstronauts(
        [FromQuery] int? missionId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var results = await _astronautService.GetAstronautsPagedAsync(missionId, pageNumber, pageSize, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Retrieves the profile of the currently authenticated astronaut (AstronautOnly).
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.AstronautOnly)]
    [ProducesResponseType(typeof(AstronautDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var astronautId = _currentUserService.AstronautId;
        if (astronautId == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
        }

        var astronaut = await _astronautService.GetAstronautByIdAsync(astronautId.Value, cancellationToken);
        if (astronaut == null)
        {
            return NotFound(new { message = "Astronaut profile not found." });
        }

        return Ok(astronaut);
    }

    /// <summary>
    /// Retrieves an astronaut profile by ID. Astronauts can view only themselves; monitors can view any.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(AstronautDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || myAstronautId.Value != id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own profile." });
            }
        }

        var astronaut = await _astronautService.GetAstronautByIdAsync(id, cancellationToken);
        if (astronaut == null)
        {
            return NotFound(new { message = $"Astronaut with ID {id} not found." });
        }

        return Ok(astronaut);
    }

    /// <summary>
    /// Updates profile fields of an astronaut (MonitorOnly). Identity and system fields cannot be modified.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(AstronautDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAstronaut(int id, [FromBody] UpdateAstronautRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var result = await _astronautService.UpdateAstronautAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivates an astronaut: locks Identity account, revokes refresh tokens, sets IsActive to false (MonitorOnly).
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAstronaut(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _astronautService.DeactivateAstronautAsync(id, cancellationToken);
            return Ok(new { message = "Astronaut successfully deactivated." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

