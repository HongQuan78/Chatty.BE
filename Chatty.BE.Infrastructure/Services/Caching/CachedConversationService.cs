using Chatty.BE.Application.DTOs.Conversations;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Config.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace Chatty.BE.Infrastructure.Services.Caching;

public sealed class CachedConversationService(
    ConversationService inner,
    IDistributedCache cache,
    RedisCacheOptions cacheOptions
) : IConversationService
{
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(Math.Max(10, cacheOptions.ConversationCacheSeconds));

    public async Task<ConversationDto> CreatePrivateConversationAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken ct = default
    )
    {
        var conversation = await inner.CreatePrivateConversationAsync(userAId, userBId, ct);
        await InvalidateConversationReadsAsync(conversation.Id, new[] { userAId, userBId }, ct);
        return conversation;
    }

    public async Task<ConversationDto> CreateGroupConversationAsync(
        Guid ownerId,
        string name,
        IEnumerable<Guid> participantIds,
        CancellationToken ct = default
    )
    {
        var ids = participantIds.Distinct().ToList();
        var conversation = await inner.CreateGroupConversationAsync(ownerId, name, ids, ct);
        ids.Add(ownerId);
        await InvalidateConversationReadsAsync(conversation.Id, ids, ct);
        return conversation;
    }

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var key = $"conversations:user:{userId:N}";
        var cached = await cache.GetAsync<List<ConversationDto>>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var conversations = await inner.GetConversationsForUserAsync(userId, ct);
        await cache.SetAsync(key, conversations, _ttl, ct);
        return conversations;
    }

    public async Task<ConversationDto?> GetByIdAsync(Guid conversationId, CancellationToken ct = default)
    {
        var key = $"conversations:id:{conversationId:N}";
        var cached = await cache.GetAsync<ConversationDto?>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var conversation = await inner.GetByIdAsync(conversationId, ct);
        if (conversation is not null)
        {
            await cache.SetAsync(key, conversation, _ttl, ct);
        }

        return conversation;
    }

    public async Task AddParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
    {
        await inner.AddParticipantAsync(conversationId, userId, ct);
        await InvalidateConversationReadsAsync(conversationId, new[] { userId }, ct);
    }

    public async Task RemoveParticipantAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
    {
        await inner.RemoveParticipantAsync(conversationId, userId, ct);
        await InvalidateConversationReadsAsync(conversationId, new[] { userId }, ct);
    }

    public async Task<bool> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var key = $"conversations:{conversationId:N}:member:{userId:N}";
        var cached = await cache.GetAsync<bool?>(key, ct);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        var result = await inner.UserIsInConversationAsync(conversationId, userId, ct);
        await cache.SetAsync(key, result, _ttl, ct);
        return result;
    }

    private async Task InvalidateConversationReadsAsync(
        Guid conversationId,
        IEnumerable<Guid> userIds,
        CancellationToken ct
    )
    {
        await cache.RemoveAsync($"conversations:id:{conversationId:N}", ct);

        foreach (var userId in userIds.Distinct())
        {
            await cache.RemoveAsync($"conversations:user:{userId:N}", ct);
            await cache.RemoveAsync($"conversations:{conversationId:N}:member:{userId:N}", ct);
        }
    }
}
