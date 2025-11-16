using Chatty.BE.Infrastructure.DependencyInjection;
using Chatty.BE.Infrastructure.SignalR;
using DotNetEnv;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFilePath))
{
    Env.Load(envFilePath);
}

var defaultConnection = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
if (!string.IsNullOrWhiteSpace(defaultConnection))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = defaultConnection;
}

// Add services
builder.Services.AddInfrastructure(builder.Configuration);
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

app.UseRouting();

app.UseAuthorization();

// Map Hub
app.MapHub<ChatHub>("/hubs/chat");

// Map Controllers
app.MapControllers();

app.Run();
