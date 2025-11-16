using Chatty.BE.Application.Implements;
using Chatty.BE.Application.Interfaces.Repositories;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Mappings;
using Chatty.BE.Infrastructure.Persistence;
using Chatty.BE.Infrastructure.Repositories;
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();

        return services;
    }
}
