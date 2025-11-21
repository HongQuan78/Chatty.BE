using Chatty.BE.Infrastructure.Config;
using Chatty.BE.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace Chatty.BE.Infrastructure.DependencyInjection;

internal static class JwtBuilder
{
    public static JwtOptions BuildJwtOptions(IConfiguration configuration)
    {
        var privateKey = ConfigurationHelpers.GetString(
            configuration,
            "JWT_PRIVATE_KEY",
            "Jwt:PrivateKey",
            required: false
        );
        var symmetricKey = ConfigurationHelpers.GetString(
            configuration,
            "JWT_SECRET",
            "Jwt:Secret",
            required: false
        );

        if (string.IsNullOrWhiteSpace(privateKey) && string.IsNullOrWhiteSpace(symmetricKey))
        {
            throw new InvalidOperationException(
                "JWT_PRIVATE_KEY or JWT_SECRET must be provided via environment variables or configuration."
            );
        }

        return new JwtOptions
        {
            Issuer = ConfigurationHelpers.GetString(
                configuration,
                "JWT_ISSUER",
                "Jwt:Issuer",
                required: false,
                defaultValue: "Chatty.BE"
            ),
            Audience = ConfigurationHelpers.GetString(
                configuration,
                "JWT_AUDIENCE",
                "Jwt:Audience",
                required: false,
                defaultValue: "Chatty.BE.Clients"
            ),
            AccessTokenLifetime = TimeSpan.FromSeconds(
                ConfigurationHelpers.GetInt(
                    configuration,
                    "ACCESS_TOKEN_EXP_SECONDS",
                    "Jwt:AccessTokenSeconds",
                    900
                )
            ),
            RefreshTokenLifetime = TimeSpan.FromSeconds(
                ConfigurationHelpers.GetInt(
                    configuration,
                    "REFRESH_TOKEN_EXP_SECONDS",
                    "Jwt:RefreshTokenSeconds",
                    60 * 60 * 24 * 30
                )
            ),
            PrivateKey = privateKey,
            PublicKey = ConfigurationHelpers.GetString(
                configuration,
                "JWT_PUBLIC_KEY",
                "Jwt:PublicKey",
                required: false
            ),
            SymmetricKey = symmetricKey,
        };
    }
}
