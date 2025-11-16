using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);

    Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<IReadOnlyList<User>> SearchUsersAsync(string keyword, CancellationToken ct = default);

    Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default);

    Task<bool> IsUserNameTakenAsync(string userName, CancellationToken ct = default);

    Task<User> UpdateProfileAsync(
        Guid userId,
        string? displayName,
        string? avatarUrl,
        string? bio,
        CancellationToken ct = default
    );
}
