using Chatty.BE.Application.DTOs.MessageAttachments;
using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Chatty.BE.Infrastructure.SignalR;

public class SignalRNotificationService(IHubContext<ChatHub, IChatClient> hubContext)
    : INotificationService
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext = hubContext;

    public async Task NotifyMessageSentAsync(
        Message message,
        IEnumerable<Guid> recipientUserIds,
        CancellationToken ct = default
    )
    {
        // Send lightweight DTO to avoid circular references when serializing.
        var payload = new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content,
            Type = message.Type,
            Status = message.Status,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            Attachments = message
                .Attachments?.Select(a => new MessageAttachmentDto
                {
                    Id = a.Id,
                    MessageId = a.MessageId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    ContentType = a.ContentType,
                    FileSizeBytes = a.FileSizeBytes,
                })
                .ToList(),
        };

        foreach (var userId in recipientUserIds)
        {
            await _hubContext.Clients.Group(userId.ToString()).ReceiveMessage(payload);
        }
    }

    public async Task NotifyMessagesReadAsync(
        Guid conversationId,
        Guid readerUserId,
        IEnumerable<Guid> affectedMessageIds,
        CancellationToken ct = default
    )
    {
        await _hubContext
            .Clients.Group(conversationId.ToString())
            .MessagesRead(conversationId, readerUserId, affectedMessageIds);
    }

    public async Task NotifyUserJoinedConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await _hubContext
            .Clients.Group(conversationId.ToString())
            .UserJoinedConversation(conversationId, userId);
    }

    public async Task NotifyUserLeftConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await _hubContext
            .Clients.Group(conversationId.ToString())
            .UserLeftConversation(conversationId, userId);
    }
}
