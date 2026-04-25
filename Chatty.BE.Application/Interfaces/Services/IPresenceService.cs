using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IPresenceService
{
    Task<Result> UpdateLastActiveAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserPresenceDto>> GetPresenceAsync(Guid userId, CancellationToken ct = default);
}
