using Chatty.BE.API.Config;
using Chatty.BE.API.Extensions;
using Chatty.BE.API.Middleware;
using Chatty.BE.Infrastructure.Config.Caching;
using Chatty.BE.Infrastructure.DependencyInjection;
using Chatty.BE.Infrastructure.SignalR;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

EnvironmentLoader.Load(builder);

// Services (IServiceCollection) 
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddCustomCors("AllowFrontend")
    .AddControllers();

// SignalR with Redis backplane for multi-instance scale-out
var redisOptions = RedisCacheOptions.Build(builder.Configuration);
var signalRBuilder = builder.Services.AddSignalR();
if (redisOptions.Enabled)
{
    signalRBuilder.AddStackExchangeRedis(redisOptions.Configuration, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal(
            $"{redisOptions.InstanceName}signalr"
        );
    });
}

// API explorer / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Chatty.BE.Infrastructure.Persistence.ChatDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

// Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseCors("AllowFrontend");

var disableHttpLogging = Environment.GetEnvironmentVariable("DISABLE_HTTP_LOGGING");
if (!string.Equals(disableHttpLogging, "1", StringComparison.OrdinalIgnoreCase))
{
    app.UseMiddleware<LoggingMiddleware>();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
app.MapControllers();

app.Run();