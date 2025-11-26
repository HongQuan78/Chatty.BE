using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Config;
using Chatty.BE.Infrastructure.Config.Upload;
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

        services.AddSingleton(JwtBuilder.BuildJwtOptions(configuration));
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();
        services.AddSingleton<IObjectMapper, ObjectMapper>();
        services.AddSingleton(CloudinaryOptionsBuilder.Build(configuration));

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IPresenceService, PresenceService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();

        return services;
    }
}
