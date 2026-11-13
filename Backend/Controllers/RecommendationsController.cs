using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Recommendations;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateRecommendationRequest> _createValidator;
    private readonly IValidator<UpdateRecommendationStatusRequest> _statusValidator;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ICurrentUserService currentUserService,
        IValidator<CreateRecommendationRequest> createValidator,
        IValidator<UpdateRecommendationStatusRequest> statusValidator)
    {
        _recommendationService = recommendationService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Retrieves recommendations. Astronauts can view only their own; monitors can view for all or specific crew members.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(IReadOnlyList<RecommendationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] int? astronautId,
        [FromQuery] bool pendingOnly = true,
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
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own recommendations." });
            }

            astronautId = myAstronautId.Value;
        }

        try
        {
            if (astronautId.HasValue)
            {
                var recommendations = await _recommendationService.GetRecommendationsForAstronautAsync(astronautId.Value, pendingOnly, cancellationToken);
                return Ok(recommendations);
            }

            var allRecommendations = await _recommendationService.GetAllRecommendationsAsync(pendingOnly, cancellationToken);
            return Ok(allRecommendations);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a recommendation by ID. Astronauts can view only their own recommendations.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(RecommendationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var recommendation = await _recommendationService.GetRecommendationByIdAsync(id, cancellationToken);
        if (recommendation == null)
        {
            return NotFound(new { message = $"Recommendation with ID {id} not found." });
        }

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null || recommendation.AstronautId != myAstronautId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own recommendations." });
            }
        }

        return Ok(recommendation);
    }

    /// <summary>
    /// Issues a new medical or operational recommendation for an astronaut (MonitorOnly).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.MonitorOnly)]
    [ProducesResponseType(typeof(RecommendationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRecommendation([FromBody] CreateRecommendationRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var result = await _recommendationService.CreateRecommendationAsync(request.AstronautId, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates the acknowledgment or completion status of a recommendation. Astronauts can update only their own.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(RecommendationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRecommendationStatusRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var astronautId = 0;
        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
            }
            astronautId = myAstronautId.Value;
        }

        try
        {
            var result = await _recommendationService.UpdateStatusAsync(id, astronautId, request, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

