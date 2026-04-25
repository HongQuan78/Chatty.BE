using System.Text.Json;
using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.Application.Implements;

public class PresenceService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IDistributedCache cache,
    ILogger<PresenceService> logger
) : IPresenceService
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan DbUpdateInterval = TimeSpan.FromMinutes(5);

    private static string GetCacheKey(Guid userId) => $"presence:{userId}";

    public async Task<Result> UpdateLastActiveAsync(Guid userId, CancellationToken ct = default)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var cacheKey = GetCacheKey(userId);

        // 1. Update Redis (Fast path)
        var presenceData = new UserPresenceDto
        {
            UserId = userId,
            IsOnline = true,
            LastActiveUtc = utcNow,
            OfflineMinutes = 0
        };

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = OnlineThreshold * 2 // Keep slightly longer than threshold
        };

        try
        {
            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(presenceData),
                options,
                ct
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update presence in Redis for user {UserId}", userId);
            // Continue to DB as fallback if Redis fails
        }

        // 2. Throttled DB Update (Slow path)
        // We only update the DB every X minutes to reduce load, or on the very first heartbeat.
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user != null)
        {
            if (!user.LastActive.HasValue || utcNow - user.LastActive > DbUpdateInterval)
            {
                user.LastActive = utcNow;
                user.UpdatedAt = utcNow;
                userRepository.Update(user);
                await unitOfWork.SaveChangesAsync(ct);

                logger.LogDebug("Synced LastActive to database for user {UserId}", userId);
            }
        }

        return Result.Success();
    }

    public async Task<Result<UserPresenceDto>> GetPresenceAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = GetCacheKey(userId);
        var utcNow = dateTimeProvider.UtcNow;

        // 1. Try Redis first
        try
        {
            var cached = await cache.GetStringAsync(cacheKey, ct);
            if (cached != null)
            {
                var presence = JsonSerializer.Deserialize<UserPresenceDto>(cached);
                if (presence != null)
                {
                    // Double check threshold in case of stale cache
                    if (utcNow - presence.LastActiveUtc <= OnlineThreshold)
                    {
                        return Result<UserPresenceDto>.Success(presence);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read presence from Redis for user {UserId}. Falling back to DB.", userId);
        }

        // 2. Fallback to DB
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Result<UserPresenceDto>.Failure("User not found.", "NOT_FOUND");
        }

        var lastActiveUtc = user.LastActive ?? user.LatestLogin ?? user.CreatedAt;
        var isOnline = (utcNow - lastActiveUtc) <= OnlineThreshold;

        var result = new UserPresenceDto
        {
            UserId = user.Id,
            IsOnline = isOnline,
            LastActiveUtc = lastActiveUtc,
            OfflineMinutes = isOnline ? 0 : (int)Math.Floor((utcNow - lastActiveUtc).TotalMinutes)
        };

        return Result<UserPresenceDto>.Success(result);
    }
}
