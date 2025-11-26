using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IPresenceService
{
    Task UpdateLastActiveAsync(Guid userId, CancellationToken ct = default);
    Task<UserPresenceDto?> GetPresenceAsync(Guid userId, CancellationToken ct = default);
}
