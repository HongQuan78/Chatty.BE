using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;

using Chatty.BE.Application.Common;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IMessageService
{
    Task<Result<MessageDto>> SendMessageAsync(
        Guid conversationId,
        Guid senderId,
        string content,
        MessageType type,
        IEnumerable<MessageAttachment>? attachments,
        CancellationToken ct = default
    );

    Task<Result<IReadOnlyList<MessageDto>>> GetMessagesAsync(
        Guid conversationId,
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default
    );

    Task<Result> MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken ct = default
    );

    Task<Result<int>> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );
}
