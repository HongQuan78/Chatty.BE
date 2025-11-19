using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(
        string userName,
        string email,
        string password,
        CancellationToken ct = default
    );

    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        string ipAddress,
        CancellationToken ct = default
    );

    Task<RefreshTokenResponseDto> RefreshAsync(
        RefreshTokenRequestDto request,
        string ipAddress,
        CancellationToken ct = default
    );

    Task LogoutAsync(
        Guid userId,
        string refreshToken,
        string? ipAddress = null,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<SessionDto>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken ct = default
    );

    Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default
    );

    Task LogoutAllSessionsAsync(Guid userId, string ipAddress, CancellationToken ct);
}
