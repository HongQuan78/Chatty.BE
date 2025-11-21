using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;
using Chatty.BE.Infrastructure.Extensions;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories;

public class MessageRepository(ChatDbContext context)
    : GenericRepository<Message>(context),
        IMessageRepository
{
    public async Task<IReadOnlyList<Message>> GetMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = _context
            .Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Attachments)
            .OrderByDescending(m => m.CreatedAt)
            .Paginate(page, pageSize);

        return await query.ToListAsync(ct);
    }

    public Task<Message?> GetLastMessageAsync(Guid conversationId, CancellationToken ct = default)
    {
        return _context
            .Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public Task<int> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        return _context
            .MessageReceipts.AsNoTracking()
            .Where(r =>
                r.UserId == userId
                && r.Message.ConversationId == conversationId
                && r.Status != MessageStatus.Read
            )
            .CountAsync(ct);
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAfterAsync(
        Guid conversationId,
        Guid lastMessageId,
        CancellationToken ct = default
    )
    {
        var referenceTimestamp = await _context
            .Messages.Where(m => m.Id == lastMessageId && m.ConversationId == conversationId)
            .Select(m => (DateTime?)m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (!referenceTimestamp.HasValue)
        {
            return [];
        }

        return await _context
            .Messages.AsNoTracking()
            .Where(m =>
                m.ConversationId == conversationId && m.CreatedAt > referenceTimestamp.Value
            )
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }
}
