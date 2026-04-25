using Chatty.BE.Application.Common;
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

    public async Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var key = $"users:id:{userId:N}";
        var cached = await cache.GetAsync<UserDto?>(key, ct);
        if (cached is not null)
        {
            return Result<UserDto>.Success(cached);
        }

        var result = await inner.GetByIdAsync(userId, ct);
        if (result.IsSuccess)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
        }

        return result;
    }

    public async Task<Result<UserDto>> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var normalized = userName.Trim().ToLowerInvariant();
        var key = $"users:username:{normalized}";
        var cached = await cache.GetAsync<UserDto?>(key, ct);
        if (cached is not null)
        {
            return Result<UserDto>.Success(cached);
        }

        var result = await inner.GetByUserNameAsync(userName, ct);
        if (result.IsSuccess)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
            await cache.SetAsync($"users:id:{result.Value!.Id:N}", result.Value, _ttl, ct);
        }

        return result;
    }

    public async Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var key = $"users:email:{normalized}";
        var cached = await cache.GetAsync<UserDto?>(key, ct);
        if (cached is not null)
        {
            return Result<UserDto>.Success(cached);
        }

        var result = await inner.GetByEmailAsync(email, ct);
        if (result.IsSuccess)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
            await cache.SetAsync($"users:id:{result.Value!.Id:N}", result.Value, _ttl, ct);
        }

        return result;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> SearchUsersAsync(
        string keyword,
        CancellationToken ct = default
    )
    {
        var normalized = keyword.Trim().ToLowerInvariant();
        var key = $"users:search:{normalized}";
        var cached = await cache.GetAsync<List<UserDto>>(key, ct);
        if (cached is not null)
        {
            return Result<IReadOnlyList<UserDto>>.Success(cached);
        }

        var result = await inner.SearchUsersAsync(keyword, ct);
        if (result.IsSuccess)
        {
            await cache.SetAsync(key, result.Value, _ttl, ct);
        }

        return result;
    }

    public Task<Result<bool>> IsEmailTakenAsync(string email, CancellationToken ct = default) =>
        inner.IsEmailTakenAsync(email, ct);

    public Task<Result<bool>> IsUserNameTakenAsync(string userName, CancellationToken ct = default) =>
        inner.IsUserNameTakenAsync(userName, ct);

    public async Task<Result<UserDto>> UpdateProfileAsync(
        UpdateUserProfileRequest request,
        CancellationToken ct = default
    )
    {
        // Obtener el perfil actual antes de actualizar para invalidar todos los cache keys
        var current = await inner.GetByIdAsync(request.UserId, ct);
        var result = await inner.UpdateProfileAsync(request, ct);

        if (result.IsSuccess)
        {
            var updated = result.Value!;
            await cache.RemoveAsync($"users:id:{request.UserId:N}", ct);
            await cache.RemoveAsync($"users:username:{updated.UserName.Trim().ToLowerInvariant()}", ct);
            await cache.RemoveAsync($"users:email:{updated.Email.Trim().ToLowerInvariant()}", ct);

            // Invalidar las keys del perfil previo si los datos cambiaron
            if (current.IsSuccess)
            {
                var prev = current.Value!;
                await cache.RemoveAsync($"users:username:{prev.UserName.Trim().ToLowerInvariant()}", ct);
                await cache.RemoveAsync($"users:email:{prev.Email.Trim().ToLowerInvariant()}", ct);
            }
        }

        return result;
    }
}
