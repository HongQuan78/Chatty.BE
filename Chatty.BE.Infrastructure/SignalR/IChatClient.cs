using Chatty.BE.Application.DTOs.Messages;

namespace Chatty.BE.Infrastructure.SignalR;

/// <summary>
/// Contract cho client SignalR
/// </summary>
public interface IChatClient
{
    Task ReceiveMessage(MessageDto message);

    Task MessagesRead(Guid conversationId, Guid readerUserId, IEnumerable<Guid> messageIds);

    Task UserJoinedConversation(Guid conversationId, Guid userId);

    Task UserLeftConversation(Guid conversationId, Guid userId);
}
