using Chatty.BE.Application.Interfaces;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Repositories;

public class MessageReceiptRepository(ChatDbContext context)
    : GenericRepository<MessageReceipt>(context),
        IMessageReceiptRepository
{
    public Task<MessageReceipt?> GetReceiptAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        return _context
            .MessageReceipts.AsNoTracking()
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId, ct);
    }

    public async Task MarkAsDeliveredAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var receipt = await GetOrCreateTrackedReceiptAsync(messageId, userId, ct);
        if (receipt.Status < MessageStatus.Delivered)
        {
            receipt.Status = MessageStatus.Delivered;
        }

        receipt.DeliveredAt ??= DateTime.UtcNow;
    }

    public async Task MarkAsReadAsync(Guid messageId, Guid userId, CancellationToken ct = default)
    {
        var receipt = await GetOrCreateTrackedReceiptAsync(messageId, userId, ct);
        receipt.DeliveredAt ??= DateTime.UtcNow;

        if (receipt.Status < MessageStatus.Read)
        {
            receipt.Status = MessageStatus.Read;
        }

        receipt.ReadAt ??= DateTime.UtcNow;
    }

    public async Task<IReadOnlyList<Guid>> GetUnreadMessageIdsForUserAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        return await _context
            .MessageReceipts.AsNoTracking()
            .Where(r =>
                r.UserId == userId
                && r.Message.ConversationId == conversationId
                && r.Status != MessageStatus.Read
            )
            .Select(r => r.MessageId)
            .ToListAsync(ct);
    }

    private async Task<MessageReceipt> GetOrCreateTrackedReceiptAsync(
        Guid messageId,
        Guid userId,
        CancellationToken ct
    )
    {
        var receipt = await _context.MessageReceipts.FirstOrDefaultAsync(
            r => r.MessageId == messageId && r.UserId == userId,
            ct
        );

        if (receipt is not null)
        {
            return receipt;
        }

        receipt = new MessageReceipt
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            UserId = userId,
            Status = MessageStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            IsDeleted = false,
        };

        await _context.MessageReceipts.AddAsync(receipt, ct);
        return receipt;
    }
}
