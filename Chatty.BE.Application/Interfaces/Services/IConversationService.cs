using Chatty.BE.Application.DTOs.Conversations;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IConversationService
{
    Task<ConversationDto> CreatePrivateConversationAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken ct = default
    );

    Task<ConversationDto> CreateGroupConversationAsync(
        Guid ownerId,
        string name,
        IEnumerable<Guid> participantIds,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<ConversationDto>> GetConversationsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    );

    Task<ConversationDto?> GetByIdAsync(Guid conversationId, CancellationToken ct = default);

    Task AddParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default);

    Task RemoveParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default);

    Task<bool> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    );
}

