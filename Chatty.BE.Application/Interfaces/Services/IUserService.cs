using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);

    Task<UserDto?> GetByUserNameAsync(string userName, CancellationToken ct = default);

    Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<IReadOnlyList<UserDto>> SearchUsersAsync(string keyword, CancellationToken ct = default);

    Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default);

    Task<bool> IsUserNameTakenAsync(string userName, CancellationToken ct = default);

    Task<UserDto> UpdateProfileAsync(
        Guid userId,
        string? displayName,
        string? avatarUrl,
        string? bio,
        CancellationToken ct = default
    );
}
