using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums; // giả sử MessageType, MessageStatus ở đây

namespace Chatty.BE.Application.Interfaces.Services;

public interface IMessageService
{
    /// <summary>
    /// Gửi tin nhắn mới trong một conversation.
    /// Có thể kèm attachments (image/file) nếu bạn implement.
    /// </summary>
    Task<Message> SendMessageAsync(
        Guid conversationId,
        Guid senderId,
        string content,
        MessageType type,
        IEnumerable<MessageAttachment>? attachments,
        CancellationToken ct = default
    );

    /// <summary>
    /// Lấy danh sách message theo conversation, có phân trang.
    /// </summary>
    Task<IReadOnlyList<Message>> GetMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default
    );

    /// <summary>
    /// Đánh dấu tất cả tin nhắn trong conversation là đã đọc bởi user.
    /// Thường sẽ update MessageReceipt.
    /// </summary>
    Task MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Đếm số tin nhắn chưa đọc trong 1 conversation cho 1 user.
    /// </summary>
    Task<int> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );
}
