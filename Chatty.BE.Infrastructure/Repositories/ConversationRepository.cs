using Chatty.BE.Application.Interfaces;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories;

public class ConversationRepository(ChatDbContext context)
    : GenericRepository<Conversation>(context),
        IConversationRepository
{
    public async Task<IReadOnlyList<Conversation>> GetConversationsOfUserAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        return await _context
            .Conversations.AsNoTracking()
            .Include(c => c.Participants)
            .ThenInclude(cp => cp.User)
            .Where(c => c.Participants.Any(cp => cp.UserId == userId))
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<Conversation?> GetPrivateConversationAsync(
        Guid userA,
        Guid userB,
        CancellationToken ct = default
    )
    {
        return _context
            .Conversations.AsNoTracking()
            .Include(c => c.Participants)
            .ThenInclude(cp => cp.User)
            .FirstOrDefaultAsync(
                c =>
                    !c.IsGroup
                    && c.Participants.Any(cp => cp.UserId == userA)
                    && c.Participants.Any(cp => cp.UserId == userB),
                ct
            );
    }

    public Task<bool> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        return _context
            .ConversationParticipants.AsNoTracking()
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId, ct);
    }

    public Task<Conversation?> GetWithParticipantsAsync(
        Guid conversationId,
        CancellationToken ct = default
    )
    {
        return _context
            .Conversations.AsNoTracking()
            .Include(c => c.Participants)
            .ThenInclude(cp => cp.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);
    }
}
