using FluentValidation;
using Chatty.BE.Application.Common;
using AutoMapper;
using Chatty.BE.Application.Implements;

using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Config.Caching;
using Chatty.BE.Infrastructure.Config;
using Chatty.BE.Infrastructure.Config.Upload;
using Chatty.BE.Infrastructure.Mappings;
using Chatty.BE.Infrastructure.Persistence;
using Chatty.BE.Infrastructure.Repositories;
using Chatty.BE.Infrastructure.Security;
using Chatty.BE.Infrastructure.Services;
using Chatty.BE.Infrastructure.Services.Caching;
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
        // Validation
        services.AddValidatorsFromAssembly(typeof(Result).Assembly);

        // DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ChatDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // AutoMapper
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);

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

        var redisOptions = RedisCacheOptions.Build(configuration);
        services.AddSingleton(redisOptions);

        if (redisOptions.Enabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.Configuration;
                options.InstanceName = redisOptions.InstanceName;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton(JwtBuilder.BuildJwtOptions(configuration));
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();
        services.AddSingleton<IObjectMapper, ObjectMapper>();

        // Cloudinary options (resolved once)
        var cloudinaryOptions = CloudinaryOptionsBuilder.Build(configuration);
        services.AddSingleton(cloudinaryOptions);

        // Conditionally register file storage strategy:
        // - If Cloudinary settings are present, use CloudinaryFileStorageService
        // - Otherwise, register a NoOpFileStorageService (Null Object) so app runs without .env
        var hasCloudinaryCfg =
            cloudinaryOptions is not null
            && !string.IsNullOrWhiteSpace(cloudinaryOptions.CloudName)
            && !string.IsNullOrWhiteSpace(cloudinaryOptions.ApiKey)
            && !string.IsNullOrWhiteSpace(cloudinaryOptions.ApiSecret);

        if (hasCloudinaryCfg)
        {
            services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, NoOpFileStorageService>();
        }

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ConversationService>();
        services.AddScoped<MessageService>();
        services.AddScoped<UserService>();
        services.AddScoped<IConversationService, CachedConversationService>();
        services.AddScoped<IMessageService, CachedMessageService>();
        services.AddScoped<IPresenceService, PresenceService>();
        services.AddScoped<IUserService, CachedUserService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();

        return services;
    }
}