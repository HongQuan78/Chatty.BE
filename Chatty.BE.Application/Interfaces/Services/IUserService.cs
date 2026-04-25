using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken ct = default);

    Task<Result<UserDto>> GetByUserNameAsync(string userName, CancellationToken ct = default);

    Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Searches for users with pagination and relevance optimization.
    /// </summary>
    Task<Result<PagedList<UserDto>>> SearchUsersAsync(
        string keyword,
        int pageIndex = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<Result<bool>> IsEmailTakenAsync(string email, CancellationToken ct = default);

    Task<Result<bool>> IsUserNameTakenAsync(string userName, CancellationToken ct = default);

    Task<Result<UserDto>> UpdateProfileAsync(
        UpdateUserProfileRequest request,
        CancellationToken ct = default
    );
}
