using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Chatty.BE.Domain.Enums;
using Chatty.BE.Infrastructure.Config.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace Chatty.BE.Infrastructure.Services.Caching;

public sealed class CachedMessageService(
    MessageService inner,
    IDistributedCache cache,
    RedisCacheOptions cacheOptions
) : IMessageService
{
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(Math.Max(10, cacheOptions.MessageCacheSeconds));

    public async Task<MessageDto> SendMessageAsync(
        Guid conversationId,
        Guid senderId,
        string content,
        MessageType type,
        IEnumerable<MessageAttachment>? attachments,
        CancellationToken ct = default
    )
    {
        var message = await inner.SendMessageAsync(conversationId, senderId, content, type, attachments, ct);
        await cache.RemoveAsync($"messages:{conversationId:N}:page:1:size:50", ct);
        return message;
    }

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(
        Guid conversationId,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var key = $"messages:{conversationId:N}:page:{page}:size:{pageSize}";
        var cached = await cache.GetAsync<List<MessageDto>>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var messages = await inner.GetMessagesAsync(conversationId, page, pageSize, ct);
        await cache.SetAsync(key, messages, _ttl, ct);
        return messages;
    }

    public async Task MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken ct = default
    )
    {
        await inner.MarkConversationAsReadAsync(conversationId, readerUserId, ct);
        await cache.RemoveAsync($"messages:{conversationId:N}:unread:{readerUserId:N}", ct);
    }

    public async Task<int> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var key = $"messages:{conversationId:N}:unread:{userId:N}";
        var cached = await cache.GetAsync<int?>(key, ct);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        var count = await inner.CountUnreadMessagesAsync(conversationId, userId, ct);
        await cache.SetAsync(key, count, _ttl, ct);
        return count;
    }
}
