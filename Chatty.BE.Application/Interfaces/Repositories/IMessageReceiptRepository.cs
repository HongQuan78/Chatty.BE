using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Repositories
{
    public interface IMessageReceiptRepository : IGenericRepository<MessageReceipt>
    {
        Task<MessageReceipt?> GetReceiptAsync(
            Guid messageId,
            Guid userId,
            CancellationToken ct = default
        );

        Task MarkAsDeliveredAsync(Guid messageId, Guid userId, CancellationToken ct = default);

        Task MarkAsReadAsync(Guid messageId, Guid userId, CancellationToken ct = default);

        Task<IReadOnlyList<Guid>> GetUnreadMessageIdsForUserAsync(
            Guid conversationId,
            Guid userId,
            CancellationToken ct = default
        );
    }
}
