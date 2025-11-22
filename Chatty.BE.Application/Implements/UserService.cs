using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;

namespace Chatty.BE.Application.Implements;

public class UserService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IObjectMapper mapper
) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await _userRepository.GetByIdAsync(userId, ct);
        return result is null ? null : mapper.Map<UserDto>(result);
    }

    public async Task<UserDto?> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var result = await _userRepository.GetByUserNameAsync(userName, ct);
        return result is null ? null : mapper.Map<UserDto>(result);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var result = await _userRepository.GetByEmailAsync(email, ct);
        return result is null ? null : mapper.Map<UserDto>(result);
    }

    public async Task<IReadOnlyList<UserDto>> SearchUsersAsync(
        string keyword,
        CancellationToken ct = default
    )
    {
        var result = await _userRepository.SearchUsersAsync(keyword, ct);
        return mapper.Map<List<UserDto>>(result);
    }

    public Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default) =>
        _userRepository.IsEmailTakenAsync(email, ct);

    public Task<bool> IsUserNameTakenAsync(string userName, CancellationToken ct = default) =>
        _userRepository.IsUserNameTakenAsync(userName, ct);

    public async Task<UserDto> UpdateProfileAsync(
        Guid userId,
        string? displayName,
        string? avatarUrl,
        string? bio,
        CancellationToken ct = default
    )
    {
        var user =
            await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (displayName is not null)
        {
            user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        }

        if (avatarUrl is not null)
        {
            user.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        }

        if (bio is not null)
        {
            user.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        }
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<UserDto>(user);
    }
}
