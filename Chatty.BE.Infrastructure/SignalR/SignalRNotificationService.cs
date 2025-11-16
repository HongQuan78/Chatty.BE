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
        // Gửi message tới từng user (group theo userId)
        foreach (var userId in recipientUserIds)
        {
            await _hubContext.Clients.Group(userId.ToString()).ReceiveMessage(message);
        }
    }

    public async Task NotifyMessagesReadAsync(
        Guid conversationId,
        Guid readerUserId,
        IEnumerable<Guid> affectedMessageIds,
        CancellationToken ct = default
    )
    {
        // Thông báo cho những user khác trong cuộc trò chuyện
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
