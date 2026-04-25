using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Chatty.BE.Infrastructure.Security;

public sealed class JwtTokenProvider(JwtOptions options, IDateTimeProvider dateTimeProvider)
    : ITokenProvider
{
    private static readonly JsonWebTokenHandler TokenHandler = new();
    private readonly JwtOptions _options =
        options ?? throw new ArgumentNullException(nameof(options));
    private readonly SigningCredentials _signingCredentials = CreateSigningCredentials(options);

    public AccessTokenResult GenerateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = dateTimeProvider.UtcNow;
        var expires = now.Add(_options.AccessTokenLifetime);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            ["username"] = user.UserName,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add("displayName", user.DisplayName);
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _signingCredentials,
        };

        var token = TokenHandler.CreateToken(descriptor);
        return new AccessTokenResult(token, expires);
    }

    public RefreshTokenResult GenerateRefreshToken(Guid userId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert
            .ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var expires = dateTimeProvider.UtcNow.Add(_options.RefreshTokenLifetime);

        return new RefreshTokenResult(token, expires);
    }

    public string ComputeHash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var data = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(data);
        return Convert.ToBase64String(hash);
    }

    private static SigningCredentials CreateSigningCredentials(JwtOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PrivateKey))
        {
            var rsa = RSA.Create();
            var privateKey = options.PrivateKey.Replace("\\n", "\n", StringComparison.Ordinal);
            rsa.ImportFromPem(privateKey.AsSpan());

            var rsaSecurityKey = new RsaSecurityKey(rsa) { KeyId = Guid.NewGuid().ToString("N") };

            return new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);
        }

        if (!string.IsNullOrWhiteSpace(options.SymmetricKey))
        {
            var symmetricKey = Encoding.UTF8.GetBytes(options.SymmetricKey);
            var securityKey = new SymmetricSecurityKey(symmetricKey)
            {
                KeyId = Guid.NewGuid().ToString("N"),
            };
            return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        }

        throw new InvalidOperationException(
            "JWT signing credentials are not configured via environment variables."
        );
    }
}
