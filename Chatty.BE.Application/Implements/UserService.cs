using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Implements;

public class UserService(IUserRepository userRepository, IUnitOfWork unitOfWork) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default) =>
        _userRepository.GetByIdAsync(userId, ct);

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default) =>
        _userRepository.GetByUserNameAsync(userName, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _userRepository.GetByEmailAsync(email, ct);

    public Task<IReadOnlyList<User>> SearchUsersAsync(
        string keyword,
        CancellationToken ct = default
    ) => _userRepository.SearchUsersAsync(keyword, ct);

    public Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default) =>
        _userRepository.IsEmailTakenAsync(email, ct);

    public Task<bool> IsUserNameTakenAsync(string userName, CancellationToken ct = default) =>
        _userRepository.IsUserNameTakenAsync(userName, ct);

    public async Task<User> UpdateProfileAsync(
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

        return user;
    }
}
