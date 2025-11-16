using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Repositories
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<IReadOnlyList<Conversation>> GetConversationsOfUserAsync(
            Guid userId,
            CancellationToken ct = default
        );

        Task<Conversation?> GetPrivateConversationAsync(
            Guid userA,
            Guid userB,
            CancellationToken ct = default
        );

        Task<bool> UserIsInConversationAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken ct = default
        );

        Task<Conversation?> GetWithParticipantsAsync(
            Guid conversationId,
            CancellationToken ct = default
        );
    }
}
