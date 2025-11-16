namespace Chatty.BE.Application.DTOs.Auth;

public sealed record SessionDto(
    Guid TokenId,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? CreatedByIp,
    bool IsRevoked,
    bool IsReused
);
