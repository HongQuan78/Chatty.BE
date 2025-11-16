using Chatty.BE.Domain.Entities;

namespace Chatty.BE.Application.Interfaces.Services;

public interface ITokenProvider
{
    AccessTokenResult GenerateAccessToken(User user);

    RefreshTokenResult GenerateRefreshToken(Guid userId);

    string ComputeHash(string token);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);

public sealed record RefreshTokenResult(string Token, DateTime ExpiresAt);
