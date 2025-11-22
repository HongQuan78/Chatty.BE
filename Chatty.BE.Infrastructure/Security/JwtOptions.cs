namespace Chatty.BE.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "Chatty.BE";

    public string Audience { get; init; } = "Chatty.Clients";

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(30);

    public string? PrivateKey { get; init; }

    public string? PublicKey { get; init; }

    public string? SymmetricKey { get; init; }
}
