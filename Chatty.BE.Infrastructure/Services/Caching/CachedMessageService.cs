using Chatty.BE.Application.Common;
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

    public async Task<Result<MessageDto>> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.SendMessageAsync(request, ct);
        if (result.IsSuccess)
        {
            await cache.RemoveAsync($"messages:{request.ConversationId:N}:page:1:size:50", ct);
        }
        return result;
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> GetMessagesAsync(
        Guid conversationId,
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        // Se delega siempre al inner para que valide la pertenencia del usuario.
        // Solo se cachea el resultado exitoso por usuario para evitar filtrar datos no autorizados.
        var key = $"messages:{conversationId:N}:user:{userId:N}:page:{page}:size:{pageSize}";
        var cached = await cache.GetAsync<List<MessageDto>>(key, ct);
        if (cached is not null)
        {
            return Result<IReadOnlyList<MessageDto>>.Success(cached);
        }

        var result = await inner.GetMessagesAsync(conversationId, userId, page, pageSize, ct);
        if (result.IsSuccess && result.Value is not null)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
        }
        return result;
    }

    public async Task<Result> MarkConversationAsReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken ct = default
    )
    {
        var result = await inner.MarkConversationAsReadAsync(conversationId, readerUserId, ct);
        if (result.IsSuccess)
        {
            await cache.RemoveAsync($"messages:{conversationId:N}:unread:{readerUserId:N}", ct);
        }
        return result;
    }

    public async Task<Result<int>> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        var key = $"messages:{conversationId:N}:unread:{userId:N}";
        var cached = await cache.GetAsync<int?>(key, ct);
        if (cached.HasValue)
        {
            return Result<int>.Success(cached.Value);
        }

        var result = await inner.CountUnreadMessagesAsync(conversationId, userId, ct);
        if (result.IsSuccess)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
        }
        return result;
    }
}
