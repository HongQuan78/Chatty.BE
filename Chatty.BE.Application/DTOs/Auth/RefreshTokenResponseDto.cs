namespace Chatty.BE.Application.DTOs.Auth;

public sealed record RefreshTokenResponseDto(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn
);
