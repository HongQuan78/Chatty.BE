using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Conversations;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IConversationService
{
    Task<Result<ConversationDto>> CreatePrivateConversationAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken ct = default
    );

    Task<Result<ConversationDto>> CreateGroupConversationAsync(
        Guid ownerId,
        string name,
        IEnumerable<Guid> participantIds,
        CancellationToken ct = default
    );

    Task<Result<IReadOnlyList<ConversationDto>>> GetConversationsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    );

    Task<Result<ConversationDto>> GetByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );

    Task<Result> AddParticipantAsync(
        Guid conversationId,
        Guid userId,
        Guid actorId,
        CancellationToken ct = default
    );

    Task<Result> RemoveParticipantAsync(
        Guid conversationId,
        Guid userId,
        Guid actorId,
        CancellationToken ct = default
    );

    Task<Result<bool>> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );
}

