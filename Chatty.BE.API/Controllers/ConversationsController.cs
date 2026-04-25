using Chatty.BE.API.Contracts.Conversations;
using Chatty.BE.API.Extensions;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ConversationsController(IConversationService conversationService)
    : ControllerBase
{
    [HttpPost("private")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePrivateConversation(
        [FromBody] CreatePrivateConversationRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (request.UserAId != currentUserId && request.UserBId != currentUserId)
        {
            return Forbid();
        }

        var result = await conversationService.CreatePrivateConversationAsync(
            request.UserAId,
            request.UserBId,
            ct
        );

        return result.ToActionResult(this, conversation =>
            CreatedAtAction(nameof(GetConversationById), new { id = conversation.Id }, conversation)
        );
    }

    [HttpPost("group")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGroupConversation(
        [FromBody] CreateGroupConversationRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (request.OwnerId != currentUserId)
        {
            return Forbid();
        }

        var result = await conversationService.CreateGroupConversationAsync(
            request.OwnerId,
            request.Name,
            request.ParticipantIds,
            ct
        );

        return result.ToActionResult(this, conversation =>
            CreatedAtAction(nameof(GetConversationById), new { id = conversation.Id }, conversation)
        );
    }

    [HttpPost("{id:guid}/participants")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddParticipant(
        [FromRoute] Guid id,
        [FromBody] AddParticipantRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        var result = await conversationService.AddParticipantAsync(id, request.UserId, currentUserId, ct);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveParticipant(
        [FromRoute] Guid id,
        [FromRoute] Guid userId,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        var result = await conversationService.RemoveParticipantAsync(id, userId, currentUserId, ct);
        return result.ToActionResult(this);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConversationsForUser(
        [FromQuery] Guid userId,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (userId != Guid.Empty && userId != currentUserId)
        {
            return Forbid();
        }

        var result = await conversationService.GetConversationsForUserAsync(currentUserId, ct);
        return result.ToActionResult(this, conversations => Ok(conversations));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConversationById([FromRoute] Guid id, CancellationToken ct)
    {
        var currentUserId = User.GetUserId();
        var result = await conversationService.GetByIdAsync(id, currentUserId, ct);
        return result.ToActionResult(this, conversation => Ok(conversation));
    }
}
