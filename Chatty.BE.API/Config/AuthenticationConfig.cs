using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Chatty.BE.API.Config;

public static class AuthenticationConfig
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwtOptions = BuildJwtValidationOptions(configuration);

        var signingKey = CreateValidationSigningKey(jwtOptions);

        services
            .AddAuthentication("Bearer")
            .AddJwtBearer(
                "Bearer",
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = signingKey,
                    };

                    // Allow SignalR clients to pass the access token via query string for WebSockets/SSE.
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (
                                !string.IsNullOrWhiteSpace(accessToken)
                                && path.StartsWithSegments("/hubs")
                            )
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        },
                    };
                }
            );

        return services;
    }

    private static JwtValidationOptions BuildJwtValidationOptions(IConfiguration configuration)
    {
        return new JwtValidationOptions
        {
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? configuration["Jwt:Issuer"]
                ?? "Chatty.BE",
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? configuration["Jwt:Audience"]
                ?? "Chatty.BE.Clients",
            PublicKey = Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY")
                ?? configuration["Jwt:PublicKey"],
            SymmetricKey = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? configuration["Jwt:Secret"],
        };
    }

    private static SecurityKey CreateValidationSigningKey(JwtValidationOptions jwtOptions)
    {
        if (!string.IsNullOrWhiteSpace(jwtOptions.PublicKey))
        {
            var rsa = RSA.Create();
            var publicKey = jwtOptions.PublicKey.Replace("\\n", "\n", StringComparison.Ordinal);
            rsa.ImportFromPem(publicKey.AsSpan());
            return new RsaSecurityKey(rsa);
        }

        if (!string.IsNullOrWhiteSpace(jwtOptions.SymmetricKey))
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SymmetricKey));
        }

        throw new InvalidOperationException(
            "JWT validation key is missing. Configure JWT_PUBLIC_KEY or JWT_SECRET."
        );
    }

    private sealed class JwtValidationOptions
    {
        public string Issuer { get; init; } = default!;

        public string Audience { get; init; } = default!;

        public string? PublicKey { get; init; }

        public string? SymmetricKey { get; init; }
    }
}
