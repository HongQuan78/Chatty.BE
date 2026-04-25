using Chatty.BE.API.Contracts.Messages;
using Chatty.BE.API.Extensions;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppSendMessageRequest = Chatty.BE.Application.DTOs.Messages.SendMessageRequest;
using Chatty.BE.Application.DTOs.MessageAttachments;

namespace Chatty.BE.API.Controllers;

[ApiController]
[Route("api/conversations/{conversationId:guid}/messages")]
[Authorize]
public sealed class MessagesController(IMessageService messageService) : ControllerBase
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
        if (request.SenderId != currentUserId)
        {
            return Forbid();
        }

        var result = await messageService.SendMessageAsync(
            new AppSendMessageRequest
            {
                ConversationId = conversationId,
                SenderId = currentUserId,
                Content = request.Content,
                Type = request.Type,
                Attachments = request.Attachments?.Select(a => new CreateMessageAttachmentRequest
                {
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    ContentType = a.ContentType,
                    FileSizeBytes = a.FileSizeBytes
                }).ToList()
            },
            ct
        );

        return result.ToActionResult(this, message =>
            CreatedAtAction(nameof(GetMessages), new { conversationId }, message)
        );
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
        return result.ToActionResult(this, messages => Ok(messages));
    }

    [HttpPut("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsRead(Guid conversationId, CancellationToken ct)
    {
        var currentUserId = User.GetUserId();
        var result = await messageService.MarkConversationAsReadAsync(conversationId, currentUserId, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnreadCount(Guid conversationId, CancellationToken ct)
    {
        var currentUserId = User.GetUserId();
        var result = await messageService.CountUnreadMessagesAsync(conversationId, currentUserId, ct);
        return result.ToActionResult(this, count => Ok(new GetUnreadCount { Count = count }));
    }
}
