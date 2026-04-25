using Chatty.BE.API.Contracts.Messages;
using Chatty.BE.API.Extensions;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Controllers;

[ApiController]
[Route("api/conversations/{conversationId:guid}/messages")]
[Authorize]
public sealed class MessagesController(
    IMessageService messageService
) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (currentUserId != request.SenderId)
        {
            return Forbid();
        }
        if (request.SenderId != Guid.Empty && request.SenderId != currentUserId)
        {
            return Forbid();
        }

        var result = await messageService.SendMessageAsync(
            conversationId,
            currentUserId,
            request.Content,
            request.Type,
            request.Attachments,
            ct
        );

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND") return NotFound(new { error = result.Error });
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(GetMessages), new { conversationId }, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default
    )
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest("Page and pageSize must be positive.");
        }

        var currentUserId = User.GetUserId();
        var result = await messageService.GetMessagesAsync(conversationId, currentUserId, page, pageSize, ct);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPut("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsRead(Guid conversationId, CancellationToken ct)
    {
        var currentUserId = User.GetUserId();
        var result = await messageService.MarkConversationAsReadAsync(conversationId, currentUserId, ct);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnreadCount(Guid conversationId, CancellationToken ct)
    {
        var currentUserId = User.GetUserId();
        var result = await messageService.CountUnreadMessagesAsync(
            conversationId,
            currentUserId,
            ct
        );

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "FORBIDDEN") return Forbid();
            return BadRequest(new { error = result.Error });
        }

        return Ok(new GetUnreadCount { Count = result.Value });
    }
}
