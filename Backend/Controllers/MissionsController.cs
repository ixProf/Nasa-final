using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Missions;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionsController : ControllerBase
{
    private readonly IMissionService _missionService;
    private readonly IAstronautService _astronautService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateMissionRequest> _createValidator;
    private readonly IValidator<UpdateMissionRequest> _updateValidator;

    public MissionsController(
        IMissionService missionService,
        IAstronautService astronautService,
        ICurrentUserService currentUserService,
        IValidator<CreateMissionRequest> createValidator,
        IValidator<UpdateMissionRequest> updateValidator)
    {
        _missionService = missionService;
        _astronautService = astronautService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Retrieves flight exploration missions. Monitors view all missions; astronauts view only their assigned mission.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(IReadOnlyList<MissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMissions(CancellationToken cancellationToken)
    {
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
            }

            var astronaut = await _astronautService.GetAstronautByIdAsync(myAstronautId.Value, cancellationToken);
            if (astronaut?.MissionId == null)
            {
                return Ok(Array.Empty<MissionDto>());
            }

            var mission = await _missionService.GetMissionByIdAsync(astronaut.MissionId.Value, cancellationToken);
            return Ok(mission != null ? new[] { mission } : Array.Empty<MissionDto>());
        }

        var allMissions = await _missionService.GetAllMissionsAsync(cancellationToken);
        return Ok(allMissions);
    }

    /// <summary>
    /// Retrieves a single mission by ID. Astronauts can view only their own assigned mission.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(MissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
            }

            var astronaut = await _astronautService.GetAstronautByIdAsync(myAstronautId.Value, cancellationToken);
            if (astronaut?.MissionId != id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own assigned mission." });
            }
        }

        var mission = await _missionService.GetMissionByIdAsync(id, cancellationToken);
        if (mission == null)
        {
            return NotFound(new { message = $"Mission with ID {id} not found." });
        }

        return Ok(mission);
    }

    /// <summary>
    /// Creates a new operational flight mission (MonitorOnly).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(MissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateMission([FromBody] CreateMissionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var result = await _missionService.CreateMissionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an operational flight mission (MonitorOnly).
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(MissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMission(int id, [FromBody] UpdateMissionRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var result = await _missionService.UpdateMissionAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an operational flight mission (MonitorOnly). Fails with 409 Conflict if astronauts are still assigned.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteMission(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _missionService.DeleteMissionAsync(id, cancellationToken);
            return NoContent();
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
    /// Assigns an astronaut to a mission (MonitorOnly).
    /// </summary>
    [HttpPost("{id:int}/astronauts/{astronautId:int}")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignAstronaut(int id, int astronautId, CancellationToken cancellationToken)
    {
        try
        {
            await _missionService.AssignAstronautAsync(id, astronautId, cancellationToken);
            return Ok(new { message = "Astronaut assigned to mission successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Unassigns an astronaut from a mission (MonitorOnly).
    /// </summary>
    [HttpDelete("{id:int}/astronauts/{astronautId:int}")]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignAstronaut(int id, int astronautId, CancellationToken cancellationToken)
    {
        try
        {
            await _missionService.UnassignAstronautAsync(id, astronautId, cancellationToken);
            return Ok(new { message = "Astronaut unassigned from mission successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

