using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces
{
    public interface IConversationParticipantRepository
        : IGenericRepository<ConversationParticipant>
    {
        Task<IReadOnlyList<User>> GetParticipantsAsync(
            Guid conversationId,
            CancellationToken ct = default
        );

        Task<bool> IsParticipantAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken ct = default
        );

        Task AddParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default);

        Task RemoveParticipantAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken ct = default
        );
    }
}
