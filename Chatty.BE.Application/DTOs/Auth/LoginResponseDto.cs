namespace Chatty.BE.Application.DTOs.Auth;

public sealed record LoginResponseDto(
    Guid UserId,
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn
);
