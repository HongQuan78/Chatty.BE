using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Repositories
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<IReadOnlyList<Message>> GetMessagesAsync(
            Guid conversationId,
            int page,
            int pageSize,
            CancellationToken ct = default
        );

        Task<Message?> GetLastMessageAsync(Guid conversationId, CancellationToken ct = default);

        Task<int> CountUnreadMessagesAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken ct = default
        );

        Task<IReadOnlyList<Message>> GetMessagesAfterAsync(
            Guid conversationId,
            Guid lastMessageId,
            CancellationToken ct = default
        );
    }
}
