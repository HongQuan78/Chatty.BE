using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Chatty.BE.Infrastructure.Persistence;

public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        // Try to read from environment variables or hardcode for dev
        var connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION") 
            ?? "Server=sqlserver;Database=ChattyDb;User Id=sa;Password=Chatty!@#123;TrustServerCertificate=True;";

        // When running locally outside docker, use localhost
        if (connectionString.Contains("Server=sqlserver"))
        {
            // If we are not in docker (no sqlserver host), fallback to localhost
            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true" && 
                Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_DOCKER") != "true")
            {
                // We'll just keep it simple, if running via dev.sh we can pass the connection string
            }
        }

        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ChatDbContext(optionsBuilder.Options);
    }
}
