using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Repositories
{
    public interface IMessageAttachmentRepository : IGenericRepository<MessageAttachment>
    {
        Task<IReadOnlyList<MessageAttachment>> GetAttachmentsByMessageIdAsync(
            Guid messageId,
            CancellationToken ct = default
        );
    }
}
