using Chatty.BE.Application.Interfaces;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories;

public class ConversationParticipantRepository(ChatDbContext context)
    : GenericRepository<ConversationParticipant>(context),
        IConversationParticipantRepository
{
    public async Task<IReadOnlyList<User>> GetParticipantsAsync(
        Guid conversationId,
        CancellationToken ct = default
    )
    {
        return await _context
            .ConversationParticipants.AsNoTracking()
            .Where(cp => cp.ConversationId == conversationId)
            .Include(cp => cp.User)
            .Select(cp => cp.User)
            .ToListAsync(ct);
    }

    public Task<bool> IsParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        return _context
            .ConversationParticipants.AsNoTracking()
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId, ct);
    }

    public async Task AddParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        if (await IsParticipantAsync(conversationId, userId, ct))
        {
            return;
        }

        var participant = new ConversationParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            UserId = userId,
            IsAdmin = false,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            IsDeleted = false,
        };

        await _context.ConversationParticipants.AddAsync(participant, ct);
    }

    public async Task RemoveParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var participant = await _context.ConversationParticipants.FirstOrDefaultAsync(
            cp => cp.ConversationId == conversationId && cp.UserId == userId,
            ct
        );

        if (participant is null)
        {
            return;
        }

        _context.ConversationParticipants.Remove(participant);
    }
}
