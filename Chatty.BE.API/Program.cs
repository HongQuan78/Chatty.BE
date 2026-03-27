using Chatty.BE.API.Config;
using Chatty.BE.API.Extensions;
using Chatty.BE.API.Middleware;
using Chatty.BE.Infrastructure.DependencyInjection;
using Chatty.BE.Infrastructure.SignalR;

var builder = WebApplication.CreateBuilder(args);

EnvironmentLoader.Load(builder);

// Services (IServiceCollection) 
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddCustomCors("AllowFrontend")
    .AddControllers();

//SignalR   
builder.Services.AddSignalR();

// API explorer / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig();

var app = builder.Build();

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

app.MapHub<ChatHub>("/hubs/chat");
app.MapControllers();

app.Run();