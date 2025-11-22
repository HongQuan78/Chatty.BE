using Chatty.BE.API.Config;
using Chatty.BE.API.Middleware;
using Chatty.BE.Infrastructure.DependencyInjection;
using Chatty.BE.Infrastructure.SignalR;

var builder = WebApplication.CreateBuilder(args);

EnvironmentLoader.Load(builder);

// Add services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerConfig();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();

// Swagger BEFORE Build()
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRouting();

var disableHttpLogging = Environment.GetEnvironmentVariable("DISABLE_HTTP_LOGGING");
if (!string.Equals(disableHttpLogging, "1", StringComparison.OrdinalIgnoreCase))
{
    app.UseMiddleware<LoggingMiddleware>();
}
app.UseAuthentication();
app.UseAuthorization();

// Map Hub
app.MapHub<ChatHub>("/hubs/chat");

// Map Controllers
app.MapControllers();

app.Run();
