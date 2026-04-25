using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Extensions;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using FluentValidation;

namespace Chatty.BE.Application.Implements;

public class UserService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IObjectMapper mapper,
    IDateTimeProvider dateTimeProvider,
    IValidator<UpdateUserProfileRequest> validator
) : IUserService
{
    public async Task<Result<UserDto>> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.", "NOT_FOUND");
        }

        return Result<UserDto>.Success(mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto>> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var user = await userRepository.GetByUserNameAsync(userName, ct);
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.", "NOT_FOUND");
        }

        return Result<UserDto>.Success(mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(email, ct);
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.", "NOT_FOUND");
        }

        return Result<UserDto>.Success(mapper.Map<UserDto>(user));
    }

    public async Task<Result<IReadOnlyList<UserDto>>> SearchUsersAsync(
        string keyword,
        CancellationToken ct = default
    )
    {
        var users = await userRepository.SearchUsersAsync(keyword, ct);
        return Result<IReadOnlyList<UserDto>>.Success(mapper.Map<List<UserDto>>(users));
    }

    public async Task<Result<bool>> IsEmailTakenAsync(string email, CancellationToken ct = default)
    {
        var taken = await userRepository.IsEmailTakenAsync(email, ct);
        return Result<bool>.Success(taken);
    }

    public async Task<Result<bool>> IsUserNameTakenAsync(string userName, CancellationToken ct = default)
    {
        var taken = await userRepository.IsUserNameTakenAsync(userName, ct);
        return Result<bool>.Success(taken);
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(
        UpdateUserProfileRequest request,
        CancellationToken ct = default
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<UserDto>();
        }

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.", "NOT_FOUND");
        }

        if (request.DisplayName is not null)
        {
            user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        }

        if (request.AvatarUrl is not null)
        {
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        }

        if (request.Bio is not null)
        {
            user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        }

        user.UpdatedAt = dateTimeProvider.UtcNow;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<UserDto>.Success(mapper.Map<UserDto>(user));
    }
}
