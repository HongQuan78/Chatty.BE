using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Config.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace Chatty.BE.Infrastructure.Services.Caching;

public sealed class CachedUserService(
    UserService inner,
    IDistributedCache cache,
    RedisCacheOptions cacheOptions
) : IUserService
{
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(Math.Max(10, cacheOptions.UserCacheSeconds));

    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var key = $"users:id:{userId:N}";
        var cached = await cache.GetAsync<UserDto?>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var user = await inner.GetByIdAsync(userId, ct);
        if (user is not null)
        {
            await cache.SetAsync(key, user, _ttl, ct);
        }

        return user;
    }

    public async Task<UserDto?> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var normalized = userName.Trim().ToLowerInvariant();
        var key = $"users:username:{normalized}";
        var cached = await cache.GetAsync<UserDto?>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var user = await inner.GetByUserNameAsync(userName, ct);
        if (user is not null)
        {
            await cache.SetAsync(key, user, _ttl, ct);
            await cache.SetAsync($"users:id:{user.Id:N}", user, _ttl, ct);
        }

        return user;
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var key = $"users:email:{normalized}";
        var cached = await cache.GetAsync<UserDto?>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var user = await inner.GetByEmailAsync(email, ct);
        if (user is not null)
        {
            await cache.SetAsync(key, user, _ttl, ct);
            await cache.SetAsync($"users:id:{user.Id:N}", user, _ttl, ct);
        }

        return user;
    }

    public async Task<IReadOnlyList<UserDto>> SearchUsersAsync(string keyword, CancellationToken ct = default)
    {
        var normalized = keyword.Trim().ToLowerInvariant();
        var key = $"users:search:{normalized}";
        var cached = await cache.GetAsync<List<UserDto>>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var users = await inner.SearchUsersAsync(keyword, ct);
        await cache.SetAsync(key, users, _ttl, ct);
        return users;
    }

    public Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default) =>
        inner.IsEmailTakenAsync(email, ct);

    public Task<bool> IsUserNameTakenAsync(string userName, CancellationToken ct = default) =>
        inner.IsUserNameTakenAsync(userName, ct);

    public async Task<UserDto> UpdateProfileAsync(
        Guid userId,
        string? displayName,
        string? avatarUrl,
        string? bio,
        CancellationToken ct = default
    )
    {
        var current = await inner.GetByIdAsync(userId, ct);
        var updated = await inner.UpdateProfileAsync(userId, displayName, avatarUrl, bio, ct);

        await cache.RemoveAsync($"users:id:{userId:N}", ct);
        await cache.RemoveAsync($"users:username:{updated.UserName.Trim().ToLowerInvariant()}", ct);
        await cache.RemoveAsync($"users:email:{updated.Email.Trim().ToLowerInvariant()}", ct);

        if (current is not null)
        {
            await cache.RemoveAsync($"users:username:{current.UserName.Trim().ToLowerInvariant()}", ct);
            await cache.RemoveAsync($"users:email:{current.Email.Trim().ToLowerInvariant()}", ct);
        }

        return updated;
    }
}
