using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Mappings;
using Chatty.BE.Infrastructure.Persistence;
using Chatty.BE.Infrastructure.Repositories;
using Chatty.BE.Infrastructure.Security;
using Chatty.BE.Infrastructure.Services;
using Chatty.BE.Infrastructure.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatty.BE.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ChatDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // Repositories & UnitOfWork
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageAttachmentRepository, MessageAttachmentRepository>();
        services.AddScoped<IMessageReceiptRepository, MessageReceiptRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var jwtOptions = BuildJwtOptions(configuration);
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();

        return services;
    }

    private static JwtOptions BuildJwtOptions(IConfiguration configuration)
    {
        static int ResolveDuration(string envKey, string? configValue, int defaultValue)
        {
            var envValue = Environment.GetEnvironmentVariable(envKey);
            if (int.TryParse(envValue, out var envSeconds) && envSeconds > 0)
            {
                return envSeconds;
            }
            if (int.TryParse(configValue, out var configSeconds) && configSeconds > 0)
            {
                return configSeconds;
            }
            return defaultValue;
        }

        var privateKey = Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY");
        var symmetricKey = Environment.GetEnvironmentVariable("JWT_SECRET");

        if (string.IsNullOrWhiteSpace(privateKey) && string.IsNullOrWhiteSpace(symmetricKey))
        {
            throw new InvalidOperationException(
                "JWT_PRIVATE_KEY or JWT_SECRET must be provided via environment variables."
            );
        }

        return new JwtOptions
        {
            Issuer = configuration["Jwt:Issuer"] ?? "Chatty.BE",
            Audience = configuration["Jwt:Audience"] ?? "Chatty.BE.Clients",
            AccessTokenLifetime = TimeSpan.FromSeconds(
                ResolveDuration("ACCESS_TOKEN_EXP_SECONDS", configuration["Jwt:AccessTokenSeconds"], 900)
            ),
            RefreshTokenLifetime = TimeSpan.FromSeconds(
                ResolveDuration(
                    "REFRESH_TOKEN_EXP_SECONDS",
                    configuration["Jwt:RefreshTokenSeconds"],
                    60 * 60 * 24 * 30
                )
            ),
            PrivateKey = privateKey,
            PublicKey = Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY"),
            SymmetricKey = symmetricKey,
        };
    }
}
