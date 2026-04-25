using Chatty.BE.Application.Common;
using Chatty.BE.Application.Common.Extensions;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Application.Extensions;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.Application.Implements;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IObjectMapper objectMapper,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequestDto> loginValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator,
    ILogger<AuthService> logger
) : IAuthService
{
    public async Task<Result<UserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default
    )
    {
        var validationResult = await registerValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<UserDto>();
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUserName = request.UserName.Trim();

        if (await userRepository.IsEmailTakenAsync(normalizedEmail, ct))
        {
            return Result<UserDto>.Failure("Email is already in use.", "CONFLICT");
        }

        if (await userRepository.IsUserNameTakenAsync(normalizedUserName, ct))
        {
            return Result<UserDto>.Failure("Username is already in use.", "CONFLICT");
        }

        var utcNow = dateTimeProvider.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = normalizedUserName,
            Email = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            CreatedAt = utcNow,
            UpdatedAt = null,
            LastActive = utcNow,
            LatestLogin = utcNow,
            IsDeleted = false,
        };

        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("User registered successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

        return Result<UserDto>.Success(objectMapper.Map<UserDto>(user));
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto request,
        string ipAddress,
        CancellationToken ct = default
    )
    {
        var validationResult = await loginValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult<LoginResponseDto>();
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailForUpdateAsync(normalizedEmail, ct);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for email {Email} from {IpAddress}", request.Email, ipAddress);
            return Result<LoginResponseDto>.Failure("Invalid credentials.", "UNAUTHORIZED");
        }

        var utcNow = dateTimeProvider.UtcNow;
        user.LatestLogin = utcNow;
        user.LastActive = utcNow;
        user.UpdatedAt = utcNow;
        userRepository.Update(user);

        var response = await IssueTokensAsync(user, ipAddress, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("User logged in. UserId: {UserId}, Ip: {IpAddress}", user.Id, ipAddress);

        return Result<LoginResponseDto>.Success(response);
    }

    public async Task<Result<RefreshTokenResponseDto>> RefreshAsync(
        RefreshTokenRequestDto request,
        string ipAddress,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<RefreshTokenResponseDto>.Failure("Refresh token is required.", "BAD_REQUEST");
        }

        var hashedToken = tokenProvider.ComputeHash(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(hashedToken, ct);

        if (storedToken is null)
        {
            return Result<RefreshTokenResponseDto>.Failure("Refresh token is not recognized.", "BAD_REQUEST");
        }

        if (storedToken.RevokedAt.HasValue)
        {
            logger.LogWarning("Security Alert: Refresh token reuse detected for UserId {UserId} from {IpAddress}", storedToken.UserId, ipAddress);
            await RevokeAllSessionsAsync(
                storedToken.UserId,
                "Refresh token reuse detected",
                ipAddress,
                ct
            );
            return Result<RefreshTokenResponseDto>.Failure("Refresh token has been revoked.", "BAD_REQUEST");
        }

        var utcNow = dateTimeProvider.UtcNow;
        if (storedToken.ExpiresAt <= utcNow)
        {
            storedToken.RevokedAt = utcNow;
            storedToken.ReasonRevoked = "Token expired";
            storedToken.RevokedByIp = ipAddress;
            refreshTokenRepository.Update(storedToken);
            await unitOfWork.SaveChangesAsync(ct);
            return Result<RefreshTokenResponseDto>.Failure("Refresh token expired.", "BAD_REQUEST");
        }

        var user = await userRepository.GetByIdAsync(storedToken.UserId, ct);
        if (user is null)
        {
            return Result<RefreshTokenResponseDto>.Failure("User not found for refresh token.", "NOT_FOUND");
        }

        var accessToken = tokenProvider.GenerateAccessToken(user);
        var (Entity, Token) = await CreateRefreshTokenAsync(user.Id, ipAddress, ct);

        storedToken.RevokedAt = utcNow;
        storedToken.ReasonRevoked = "Replaced by new token";
        storedToken.RevokedByIp = ipAddress;
        storedToken.ReplacedByTokenId = Entity.Id;

        refreshTokenRepository.Update(storedToken);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<RefreshTokenResponseDto>.Success(new RefreshTokenResponseDto(
            accessToken.Token,
            dateTimeProvider.SecondsUntil(accessToken.ExpiresAt),
            Token,
            dateTimeProvider.SecondsUntil(Entity.ExpiresAt)
        ));
    }

    public async Task<Result> LogoutAsync(
        Guid userId,
        string refreshToken,
        string? ipAddress,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Failure("Refresh token is required.", "BAD_REQUEST");
        }

        var utcNow = dateTimeProvider.UtcNow;
        var hashedToken = tokenProvider.ComputeHash(refreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(hashedToken, ct);

        if (storedToken is null || storedToken.UserId != userId)
        {
            return Result.Success(); // Silent success for security
        }

        if (storedToken.RevokedAt is not null)
        {
            return Result.Success();
        }

        storedToken.RevokedAt = utcNow;
        storedToken.ReasonRevoked = "User logout";
        storedToken.RevokedByIp = ipAddress;

        refreshTokenRepository.Update(storedToken);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is not null)
        {
            user.LatestLogout = utcNow;
            user.LastActive = utcNow;
            user.UpdatedAt = utcNow;
            userRepository.Update(user);
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("User logged out. UserId: {UserId}, Ip: {IpAddress}", userId, ipAddress);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var tokens = await refreshTokenRepository.GetTokensByUserIdAsync(
            userId,
            includeRevoked: false,
            ct
        );

        var utcNow = dateTimeProvider.UtcNow;
        var activeSessions = tokens
            .OrderByDescending(t => t.CreatedAt)
            .Where(t => t.ExpiresAt > utcNow)
            .Select(t => objectMapper.Map<SessionDto>(t))
            .ToList();

        return Result<IReadOnlyList<SessionDto>>.Success(activeSessions);
    }

    public async Task<Result> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken ct = default
    )
    {
        var validationResult = await changePasswordValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult();
        }

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return Result.Failure("User not found.", "NOT_FOUND");
        }

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            logger.LogWarning("Failed password change attempt for UserId {UserId}. Incorrect current password.", request.UserId);
            return Result.Failure("Current password is incorrect.", "BAD_REQUEST");
        }

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = dateTimeProvider.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Password changed successfully for UserId: {UserId}", request.UserId);

        return Result.Success();
    }

    public async Task<Result> LogoutAllSessionsAsync(Guid userId, string ipAddress, CancellationToken ct)
    {
        var utcNow = dateTimeProvider.UtcNow;
        await RevokeAllSessionsAsync(userId, "User logged out from all sessions", ipAddress, ct);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is not null)
        {
            user.LatestLogout = utcNow;
            user.LastActive = utcNow;
            user.UpdatedAt = utcNow;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(ct);
        }

        logger.LogInformation("All sessions logged out for UserId: {UserId}, Ip: {IpAddress}", userId, ipAddress);

        return Result.Success();
    }

    private async Task<LoginResponseDto> IssueTokensAsync(
        User user,
        string ipAddress,
        CancellationToken ct
    )
    {
        var accessToken = tokenProvider.GenerateAccessToken(user);
        var (Entity, Token) = await CreateRefreshTokenAsync(user.Id, ipAddress, ct);

        return new LoginResponseDto(
            user.Id,
            accessToken.Token,
            dateTimeProvider.SecondsUntil(accessToken.ExpiresAt),
            Token,
            dateTimeProvider.SecondsUntil(Entity.ExpiresAt)
        );
    }

    private async Task<(RefreshToken Entity, string Token)> CreateRefreshTokenAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken ct
    )
    {
        var refreshTokenResult = tokenProvider.GenerateRefreshToken(userId);
        var utcNow = dateTimeProvider.UtcNow;
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenProvider.ComputeHash(refreshTokenResult.Token),
            ExpiresAt = refreshTokenResult.ExpiresAt,
            CreatedAt = utcNow,
            UpdatedAt = null,
            IsDeleted = false,
            CreatedByIp = ipAddress,
        };

        await refreshTokenRepository.AddAsync(entity, ct);

        return (entity, refreshTokenResult.Token);
    }

    private async Task RevokeAllSessionsAsync(
        Guid userId,
        string reason,
        string? ipAddress,
        CancellationToken ct
    )
    {
        var tokens = await refreshTokenRepository.GetTokensByUserIdAsync(
            userId,
            includeRevoked: true,
            ct
        );

        var utcNow = dateTimeProvider.UtcNow;
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

        refreshTokenRepository.UpdateRange(tokens);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
