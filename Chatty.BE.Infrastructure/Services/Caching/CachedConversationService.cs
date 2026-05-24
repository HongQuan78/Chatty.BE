using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Conversations;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Config.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace Chatty.BE.Infrastructure.Services.Caching;

public sealed class CachedConversationService(
    ConversationService inner,
    IDistributedCache cache,
    RedisCacheOptions cacheOptions,
    IConversationParticipantRepository participantRepository
) : IConversationService
{
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(Math.Max(10, cacheOptions.ConversationCacheSeconds));

    public async Task<Result<ConversationDto>> CreatePrivateConversationAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken ct = default
    )
    {
        var result = await inner.CreatePrivateConversationAsync(userAId, userBId, ct);
        if (result.IsSuccess)
        {
            await InvalidateConversationReadsAsync(result.Value!.Id, new[] { userAId, userBId }, ct);
        }
        return result;
    }

    public async Task<Result<ConversationDto>> CreateGroupConversationAsync(
        Guid ownerId,
        string name,
        IEnumerable<Guid> participantIds,
        CancellationToken ct = default
    )
    {
        var ids = participantIds.Distinct().ToList();
        var result = await inner.CreateGroupConversationAsync(ownerId, name, ids, ct);
        if (result.IsSuccess)
        {
            ids.Add(ownerId);
            await InvalidateConversationReadsAsync(result.Value!.Id, ids, ct);
        }
        return result;
    }

    public async Task<Result<IReadOnlyList<ConversationDto>>> GetConversationsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var key = $"conversations:user:{userId:N}";
        var cached = await cache.GetAsync<List<ConversationDto>>(key, ct);
        if (cached is not null)
        {
            return Result<IReadOnlyList<ConversationDto>>.Success(cached);
        }

        var result = await inner.GetConversationsForUserAsync(userId, ct);
        if (result.IsSuccess)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
        }
        return result;
    }

    public async Task<Result<ConversationDto>> GetByIdAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        // We cache the conversation data itself, but we still need to check if the user is a participant.
        // For simplicity and correctness with the new Result pattern, we'll cache the success result.
        var key = $"conversations:id:{conversationId:N}:user:{userId:N}";
        var cached = await cache.GetAsync<ConversationDto>(key, ct);
        if (cached is not null)
        {
            return Result<ConversationDto>.Success(cached);
        }

        var result = await inner.GetByIdAsync(conversationId, userId, ct);
        if (result.IsSuccess)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
        }

        return result;
    }

    public async Task<Result> AddParticipantAsync(
        Guid conversationId,
        Guid userId,
        Guid actorId,
        CancellationToken ct = default
    )
    {
        var result = await inner.AddParticipantAsync(conversationId, userId, actorId, ct);
        if (result.IsSuccess)
        {
            var affectedUserIds = await GetParticipantIdsAsync(conversationId, ct);
            affectedUserIds.Add(userId);
            affectedUserIds.Add(actorId);

            await InvalidateConversationReadsAsync(conversationId, affectedUserIds, ct);
        }
        return result;
    }

    public async Task<Result> RemoveParticipantAsync(
        Guid conversationId,
        Guid userId,
        Guid actorId,
        CancellationToken ct = default
    )
    {
        var affectedUserIds = await GetParticipantIdsAsync(conversationId, ct);
        affectedUserIds.Add(userId);
        affectedUserIds.Add(actorId);

        var result = await inner.RemoveParticipantAsync(conversationId, userId, actorId, ct);
        if (result.IsSuccess)
        {
            await InvalidateConversationReadsAsync(conversationId, affectedUserIds, ct);
        }
        return result;
    }

    public async Task<Result<bool>> UserIsInConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var key = $"conversations:{conversationId:N}:member:{userId:N}";
        var cached = await cache.GetAsync<bool?>(key, ct);
        if (cached.HasValue)
        {
            return Result<bool>.Success(cached.Value);
        }

        var result = await inner.UserIsInConversationAsync(conversationId, userId, ct);
        if (result.IsSuccess)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
        }
        return result;
    }

    private async Task InvalidateConversationReadsAsync(
        Guid conversationId,
        IEnumerable<Guid> userIds,
        CancellationToken ct
    )
    {
        foreach (var userId in userIds.Distinct())
        {
            await cache.RemoveAsync($"conversations:id:{conversationId:N}:user:{userId:N}", ct);
            await cache.RemoveAsync($"conversations:user:{userId:N}", ct);
            await cache.RemoveAsync($"conversations:{conversationId:N}:member:{userId:N}", ct);
        }
    }

    private async Task<HashSet<Guid>> GetParticipantIdsAsync(
        Guid conversationId,
        CancellationToken ct
    )
    {
        var participants = await participantRepository.GetParticipantsAsync(conversationId, ct);
        return participants.Select(participant => participant.Id).ToHashSet();
    }
}
