using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Chatty.BE.Infrastructure.SignalR;

public class SignalRNotificationService(
    IHubContext<ChatHub, IChatClient> hubContext,
    IObjectMapper mapper)
    : INotificationService
{
    public async Task NotifyMessageSentAsync(
        Message message,
        IEnumerable<Guid> recipientUserIds,
        CancellationToken ct = default
    )
    {
        // Use IObjectMapper for consistent DTO mapping
        var payload = mapper.Map<MessageDto>(message);

        // Send to individual user groups
        foreach (var userId in recipientUserIds)
        {
            await hubContext.Clients.Group(userId.ToString()).ReceiveMessage(payload);
        }
    }

    public async Task NotifyMessagesReadAsync(
        Guid conversationId,
        Guid readerUserId,
        IEnumerable<Guid> affectedMessageIds,
        CancellationToken ct = default
    )
    {
        // Broadcast to the conversation group
        await hubContext
            .Clients.Group(conversationId.ToString())
            .MessagesRead(conversationId, readerUserId, affectedMessageIds);
    }

    public async Task NotifyUserJoinedConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await hubContext
            .Clients.Group(conversationId.ToString())
            .UserJoinedConversation(conversationId, userId);
    }

    public async Task NotifyUserLeftConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await hubContext
            .Clients.Group(conversationId.ToString())
            .UserLeftConversation(conversationId, userId);
    }
}
