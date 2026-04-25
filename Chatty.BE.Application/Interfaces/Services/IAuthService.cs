using Chatty.BE.Application.Common;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<UserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default
    );

    Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto request,
        string ipAddress,
        CancellationToken ct = default
    );

    Task<Result<RefreshTokenResponseDto>> RefreshAsync(
        RefreshTokenRequestDto request,
        string ipAddress,
        CancellationToken ct = default
    );

    Task<Result> LogoutAsync(
        Guid userId,
        string refreshToken,
        string? ipAddress = null,
        CancellationToken ct = default
    );

    Task<Result<IReadOnlyList<SessionDto>>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken ct = default
    );

    Task<Result> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken ct = default
    );

    Task<Result> LogoutAllSessionsAsync(Guid userId, string ipAddress, CancellationToken ct);
}
