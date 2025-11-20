using Chatty.BE.API.Contracts.Conversations;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ConversationsController(IConversationService conversationService)
    : ControllerBase
{
    [Authorize]
    [HttpPost("private")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePrivateConversation(
        [FromBody] CreatePrivateConversationRequest request,
        CancellationToken ct
    )
    {
        var conversation = await conversationService.CreatePrivateConversationAsync(
            request.UserAId,
            request.UserBId,
            ct
        );
        return CreatedAtAction(
            nameof(GetConversationById),
            new { id = conversation.Id },
            conversation
        );
    }

    [Authorize]
    [HttpPost("group")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateGroupConversation(
        [FromBody] CreateGroupConversationRequest request,
        CancellationToken ct
    )
    {
        var conversation = await conversationService.CreateGroupConversationAsync(
            request.OwnerId,
            request.Name,
            request.ParticipantIds,
            ct
        );
        return CreatedAtAction(
            nameof(GetConversationById),
            new { id = conversation.Id },
            conversation
        );
    }

    [Authorize]
    [HttpPost("{id:guid}/participants")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddParticipant(
        [FromRoute] Guid id,
        [FromBody] AddParticipantRequest request,
        CancellationToken ct
    )
    {
        await conversationService.AddParticipantAsync(id, request.UserId, ct);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveParticipant(
        [FromRoute] Guid id,
        [FromRoute] Guid userId,
        CancellationToken ct
    )
    {
        await conversationService.RemoveParticipantAsync(id, userId, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConversationsForUser(
        [FromQuery] Guid userId,
        CancellationToken ct
    )
    {
        var conversations = await conversationService.GetConversationsForUserAsync(userId, ct);
        return Ok(conversations);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationById([FromRoute] Guid id, CancellationToken ct)
    {
        var conversation = await conversationService.GetByIdAsync(id, ct);
        if (conversation is null)
        {
            return NotFound();
        }
        return Ok(conversation);
    }
}
