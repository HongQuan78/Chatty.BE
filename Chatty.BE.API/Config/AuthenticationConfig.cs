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
        var jwtSecret =
            Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? configuration["Jwt:Secret"]
            ?? string.Empty;
        var issuer =
            Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["Jwt:Issuer"];
        var audience =
            Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["Jwt:Audience"];

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
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSecret)
                        ),
                    };
                }
            );

        return services;
    }
}
