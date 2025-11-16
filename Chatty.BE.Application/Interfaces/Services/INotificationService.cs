using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Services;

public interface INotificationService
{
    /// <summary>
    /// Gửi realtime notification khi có tin nhắn mới.
    /// recipients có thể là toàn bộ participants trừ sender.
    /// </summary>
    Task NotifyMessageSentAsync(
        Message message,
        IEnumerable<Guid> recipientUserIds,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gửi event khi tin nhắn được đánh dấu đã đọc.
    /// </summary>
    Task NotifyMessagesReadAsync(
        Guid conversationId,
        Guid readerUserId,
        IEnumerable<Guid> affectedMessageIds,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gửi event khi user join vào conversation (group).
    /// </summary>
    Task NotifyUserJoinedConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gửi event khi user rời khỏi conversation.
    /// </summary>
    Task NotifyUserLeftConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );
}
