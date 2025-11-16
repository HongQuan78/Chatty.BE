using System.Net;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.Exceptions;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Implements;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork
) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<User> RegisterAsync(
        string userName,
        string email,
        string password,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedUserName = userName.Trim();

        if (await _userRepository.IsEmailTakenAsync(normalizedEmail, ct))
        {
            throw new AppException(HttpStatusCode.Conflict, "Email is already in use.");
        }

        if (await _userRepository.IsUserNameTakenAsync(normalizedUserName, ct))
        {
            throw new AppException(HttpStatusCode.Conflict, "Username is already in use.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = normalizedUserName,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(password),
            CreatedAt = utcNow,
            UpdatedAt = null,
            IsDeleted = false,
        };

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return user;
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        string ipAddress,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user =
            await _userRepository.GetByEmailAsync(normalizedEmail, ct)
            ?? throw new AppException(HttpStatusCode.Unauthorized, "Invalid credentials.");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AppException(HttpStatusCode.Unauthorized, "Invalid credentials.");
        }

        var response = await IssueTokensAsync(user, ipAddress, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return response;
    }

    public async Task<RefreshTokenResponseDto> RefreshAsync(
        RefreshTokenRequestDto request,
        string ipAddress,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RefreshToken);

        var hashedToken = _tokenProvider.ComputeHash(request.RefreshToken);
        var storedToken =
            await _refreshTokenRepository.GetByTokenHashAsync(hashedToken, ct)
            ?? throw new AppException(
                HttpStatusCode.BadRequest,
                "Refresh token is not recognized."
            );

        if (storedToken.RevokedAt.HasValue)
        {
            await RevokeAllSessionsAsync(
                storedToken.UserId,
                "Refresh token reuse detected",
                ipAddress,
                ct
            );
            throw new AppException(HttpStatusCode.BadRequest, "Refresh token has been revoked.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        if (storedToken.ExpiresAt <= utcNow)
        {
            storedToken.RevokedAt = utcNow;
            storedToken.ReasonRevoked = "Token expired";
            storedToken.RevokedByIp = ipAddress;
            _refreshTokenRepository.Update(storedToken);
            await _unitOfWork.SaveChangesAsync(ct);
            throw new AppException(HttpStatusCode.BadRequest, "Refresh token expired.");
        }

        var user =
            await _userRepository.GetByIdAsync(storedToken.UserId, ct)
            ?? throw new AppException(HttpStatusCode.NotFound, "User not found for refresh token.");

        var accessToken = _tokenProvider.GenerateAccessToken(user);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress, ct);

        storedToken.RevokedAt = utcNow;
        storedToken.ReasonRevoked = "Replaced by new token";
        storedToken.RevokedByIp = ipAddress;
        storedToken.ReplacedByTokenId = newRefreshToken.Entity.Id;

        _refreshTokenRepository.Update(storedToken);
        await _unitOfWork.SaveChangesAsync(ct);

        return new RefreshTokenResponseDto(
            accessToken.Token,
            CalculateSeconds(accessToken.ExpiresAt),
            newRefreshToken.Token,
            CalculateSeconds(newRefreshToken.Entity.ExpiresAt)
        );
    }

    public async Task LogoutAsync(
        Guid userId,
        string refreshToken,
        string? ipAddress,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var hashedToken = _tokenProvider.ComputeHash(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(hashedToken, ct);

        if (storedToken is null || storedToken.UserId != userId)
        {
            return;
        }

        if (storedToken.RevokedAt is not null)
        {
            return;
        }

        storedToken.RevokedAt = _dateTimeProvider.UtcNow;
        storedToken.ReasonRevoked = "User logout";
        storedToken.RevokedByIp = ipAddress;

        _refreshTokenRepository.Update(storedToken);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SessionDto>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var tokens = await _refreshTokenRepository.GetTokensByUserIdAsync(
            userId,
            includeRevoked: false,
            ct
        );

        var utcNow = _dateTimeProvider.UtcNow;
        return tokens
            .OrderByDescending(t => t.CreatedAt)
            .Where(t => t.ExpiresAt > utcNow)
            .Select(t => new SessionDto(
                t.Id,
                t.CreatedAt,
                t.ExpiresAt,
                t.CreatedByIp,
                t.RevokedAt.HasValue,
                t.IsReusedToken
            ))
            .ToList();
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var user =
            await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.UpdatedAt = _dateTimeProvider.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<LoginResponseDto> IssueTokensAsync(
        User user,
        string ipAddress,
        CancellationToken ct
    )
    {
        var accessToken = _tokenProvider.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress, ct);

        return new LoginResponseDto(
            user.Id,
            accessToken.Token,
            CalculateSeconds(accessToken.ExpiresAt),
            refreshToken.Token,
            CalculateSeconds(refreshToken.Entity.ExpiresAt)
        );
    }

    private async Task<(RefreshToken Entity, string Token)> CreateRefreshTokenAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken ct
    )
    {
        var refreshTokenResult = _tokenProvider.GenerateRefreshToken(userId);
        var utcNow = _dateTimeProvider.UtcNow;
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = _tokenProvider.ComputeHash(refreshTokenResult.Token),
            ExpiresAt = refreshTokenResult.ExpiresAt,
            CreatedAt = utcNow,
            UpdatedAt = null,
            IsDeleted = false,
            CreatedByIp = ipAddress,
        };

        await _refreshTokenRepository.AddAsync(entity, ct);

        return (entity, refreshTokenResult.Token);
    }

    private async Task RevokeAllSessionsAsync(
        Guid userId,
        string reason,
        string? ipAddress,
        CancellationToken ct
    )
    {
        var tokens = await _refreshTokenRepository.GetTokensByUserIdAsync(
            userId,
            includeRevoked: true,
            ct
        );

        var utcNow = _dateTimeProvider.UtcNow;
        foreach (var token in tokens.Where(t => !t.RevokedAt.HasValue))
        {
            token.RevokedAt = utcNow;
            token.ReasonRevoked = reason;
            token.RevokedByIp = ipAddress;
            token.IsReusedToken = true;
        }

        if (tokens.Count == 0)
        {
            return;
        }

        _refreshTokenRepository.UpdateRange(tokens);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private int CalculateSeconds(DateTime expiresAt)
    {
        var seconds = (int)(expiresAt - _dateTimeProvider.UtcNow).TotalSeconds;
        return seconds > 0 ? seconds : 0;
    }
}
