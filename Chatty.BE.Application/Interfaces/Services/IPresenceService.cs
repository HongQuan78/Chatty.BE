using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.Interfaces.Services;

/// <summary>
/// Service for managing user presence and activity status.
/// </summary>
public interface IPresenceService
{
    /// <summary>
    /// Updates the last active timestamp for a user in the cache and eventually the database.
    /// </summary>
    Task<Result> UpdateLastActiveAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the current presence status of a user.
    /// </summary>
    Task<Result<UserPresenceDto>> GetPresenceAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Populates the cache with recent activity data from the database.
    /// Used for startup warming or recovery.
    /// </summary>
    Task<Result> WarmUpCacheAsync(CancellationToken ct = default);
}
