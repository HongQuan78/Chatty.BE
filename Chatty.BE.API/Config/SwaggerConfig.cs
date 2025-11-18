using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Chatty.BE.API.Config;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Chatty.BE API", Version = "v1" });

            // JWT Bearer configuration
            c.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter the token: Bearer {token}",
                }
            );
        });

        return services;
    }
}
