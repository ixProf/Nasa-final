using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstronautHealthMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<SendChatMessageRequest> _validator;

    public ChatController(
        IChatService chatService,
        ICurrentUserService currentUserService,
        IValidator<SendChatMessageRequest> validator)
    {
        _chatService = chatService;
        _currentUserService = currentUserService;
        _validator = validator;
    }

    /// <summary>
    /// Retrieves paged communication history for an astronaut thread.
    /// Astronauts see only their own thread; flight monitors can select any astronaut.
    /// </summary>
    [HttpGet("messages")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        [FromQuery] int? astronautId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
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
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own chat messages." });
            }

            targetAstronautId = myAstronautId.Value;
        }
        else if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            if (!astronautId.HasValue || astronautId.Value <= 0)
            {
                return BadRequest(new { message = "A valid positive AstronautId must be specified by monitors." });
            }

            targetAstronautId = astronautId.Value;
        }
        else
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Role not authorized for chat access." });
        }

        try
        {
            var messages = await _chatService.GetMessagesPagedAsync(targetAstronautId, pageNumber, pageSize, cancellationToken);
            return Ok(messages);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Sends a communication message to an astronaut support thread. Idempotent on ClientGeneratedId.
    /// </summary>
    [HttpPost("messages")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Message request body is required." });
        }

        int targetAstronautId;

        if (_currentUserService.IsInRole(UserRoles.Astronaut))
        {
            var myAstronautId = _currentUserService.AstronautId;
            if (myAstronautId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronaut identity not found in token claims." });
            }

            if (request.AstronautId.HasValue && request.AstronautId.Value != 0 && request.AstronautId.Value != myAstronautId.Value)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only send messages in their own thread." });
            }

            targetAstronautId = myAstronautId.Value;
            request.AstronautId = myAstronautId.Value;
        }
        else if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            if (!request.AstronautId.HasValue || request.AstronautId.Value <= 0)
            {
                return BadRequest(new { message = "A valid target AstronautId must be specified by monitors." });
            }

            targetAstronautId = request.AstronautId.Value;
        }
        else
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Role not authorized to send messages." });
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        try
        {
            var senderUserId = _currentUserService.UserId ?? "unknown";
            var senderRole = _currentUserService.Role ?? UserRoles.Astronaut;

            var createdMessage = await _chatService.SendMessageAsync(targetAstronautId, request, senderUserId, senderRole, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, createdMessage);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Marks an individual chat message as read.
    /// </summary>
    [HttpPost("messages/{id:int}/read")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkMessageAsRead(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var readerUserId = _currentUserService.UserId ?? "unknown";
            var readerRole = _currentUserService.Role ?? UserRoles.Astronaut;

            var updated = await _chatService.MarkAsReadAsync(id, readerUserId, readerRole, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves count of unread messages for an astronaut thread.
    /// </summary>
    [HttpGet("unread-count")]
    [Authorize(Policy = AuthPolicies.Authenticated)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnreadCount(
        [FromQuery] int? astronautId,
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
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Astronauts can only view their own unread message count." });
            }

            targetAstronautId = myAstronautId.Value;
        }
        else if (_currentUserService.IsInRole(UserRoles.Monitor))
        {
            if (!astronautId.HasValue || astronautId.Value <= 0)
            {
                return BadRequest(new { message = "A valid positive AstronautId must be specified by monitors." });
            }

            targetAstronautId = astronautId.Value;
        }
        else
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Role not authorized for chat access." });
        }

        var readerRole = _currentUserService.Role ?? UserRoles.Astronaut;
        var unreadCount = await _chatService.GetUnreadCountAsync(targetAstronautId, readerRole, cancellationToken);

        return Ok(new { astronautId = targetAstronautId, unreadCount });
    }
}

