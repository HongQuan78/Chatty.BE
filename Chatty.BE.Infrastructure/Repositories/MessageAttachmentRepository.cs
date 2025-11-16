using Chatty.BE.Application.Interfaces;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories;

public class MessageAttachmentRepository(ChatDbContext context)
    : GenericRepository<MessageAttachment>(context),
        IMessageAttachmentRepository
{
    public async Task<IReadOnlyList<MessageAttachment>> GetAttachmentsByMessageIdAsync(
        Guid messageId,
        CancellationToken ct = default
    )
    {
        return await _context
            .MessageAttachments.AsNoTracking()
            .Where(a => a.MessageId == messageId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);
    }
}
