using Chatty.BE.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

// builder.Services.AddSignalR();

var app = builder.Build();

app.MapControllers();

// app.MapHub<ChatHub>("/hubs/chat"); // sau này bạn tạo Hub

app.Run();
