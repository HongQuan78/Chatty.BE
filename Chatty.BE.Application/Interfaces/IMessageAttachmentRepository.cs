using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces
{
    public interface IMessageAttachmentRepository : IGenericRepository<MessageAttachment>
    {
        Task<IReadOnlyList<MessageAttachment>> GetAttachmentsByMessageIdAsync(
            Guid messageId,
            CancellationToken ct = default
        );
    }
}
