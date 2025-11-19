using System.IO;
using System.Text;
using Chatty.BE.API.Config;
using Chatty.BE.API.Middleware;
using Chatty.BE.Infrastructure.DependencyInjection;
using Chatty.BE.Infrastructure.SignalR;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddSwaggerConfig();
builder
    .Services.AddAuthentication("Bearer")
    .AddJwtBearer(
        "Bearer",
        options =>
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
            };
        }
    );
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

app.UseAuthentication();
app.UseAuthorization();

// Map Hub
app.MapHub<ChatHub>("/hubs/chat");

// Map Controllers
app.MapControllers();

app.Run();

public partial class Program;
