using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.SymptomGuidance;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/symptoms")]
public class SymptomGuidanceController : ControllerBase
{
    private readonly ISymptomGuidanceService _symptomGuidanceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<EvaluateSymptomsRequest> _validator;

    public SymptomGuidanceController(
        ISymptomGuidanceService symptomGuidanceService,
        ICurrentUserService currentUserService,
        IValidator<EvaluateSymptomsRequest> validator)
    {
        _symptomGuidanceService = symptomGuidanceService;
        _currentUserService = currentUserService;
        _validator = validator;
    }

    /// <summary>
    /// Retrieves all observable signs and subjective symptoms in the catalog.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(IReadOnlyList<SymptomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllSymptoms(CancellationToken cancellationToken = default)
    {
        var symptoms = await _symptomGuidanceService.GetAllSymptomsAsync(cancellationToken);
        return Ok(symptoms);
    }

    /// <summary>
    /// Retrieves space adaptation conditions and guidance topics, optionally filtered by keyword search.
    /// </summary>
    [HttpGet("guidance")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(IReadOnlyList<SpaceConditionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuidanceCatalog(
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var conditions = await _symptomGuidanceService.GetAllConditionsAsync(search, cancellationToken);
        return Ok(conditions);
    }

    /// <summary>
    /// Evaluates reported symptoms, returns ranked potential space health associations, and triggers emergency alert if red flags are present.
    /// </summary>
    [HttpPost("assess")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(SymptomAssessmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssessSymptoms(
        [FromBody] EvaluateSymptomsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Assessment request body is required." });
        }

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
            }

            if (request.AstronautId != 0 && request.AstronautId != myAstronautId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only assess symptoms for themselves." });
            }

            request.AstronautId = myAstronautId.Value;
        }
        else if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            if (request.AstronautId <= 0)
            {
                return BadRequest(new { message = "A valid target AstronautId must be specified by monitors." });
            }
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        try
        {
            var result = await _symptomGuidanceService.EvaluateSymptomsAsync(request, cancellationToken);
            return Ok(result);
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
}

