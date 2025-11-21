using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(
        Guid conversationId,
        Guid senderId,
        string content,
        MessageType type,
        IEnumerable<MessageAttachment>? attachments,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<MessageDto>> GetMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default
    );

    Task MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken ct = default
    );

    Task<int> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );
}
