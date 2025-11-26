using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;

namespace Chatty.BE.Application.Implements;

public class PresenceService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider
) : IPresenceService
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan MinUpdateInterval = TimeSpan.FromSeconds(15);

    public async Task UpdateLastActiveAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return;
        }

        var utcNow = dateTimeProvider.UtcNow;
        if (user.LastActive.HasValue && utcNow - user.LastActive < MinUpdateInterval)
        {
            return;
        }

        user.LastActive = utcNow;
        user.UpdatedAt = utcNow;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<UserPresenceDto?> GetPresenceAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return null;
        }

        var utcNow = dateTimeProvider.UtcNow;
        var lastActiveUtc = user.LastActive ?? user.LatestLogin ?? user.CreatedAt;
        var isOnline = (utcNow - lastActiveUtc) <= OnlineThreshold;
        int? offlineMinutes = isOnline
            ? 0
            : (int)Math.Floor((utcNow - lastActiveUtc).TotalMinutes);

        return new UserPresenceDto
        {
            UserId = user.Id,
            IsOnline = isOnline,
            LastActiveUtc = lastActiveUtc,
            OfflineMinutes = offlineMinutes,
        };
    }
}
