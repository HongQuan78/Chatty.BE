using Chatty.BE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Chatty.BE.API.IntegrationTests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ChattyAuthTests_{Guid.NewGuid():N}";

    public AuthApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "DEFAULT_CONNECTION",
            "Server=localhost;Database=ChattyTests;Trusted_Connection=True;Encrypt=False;"
        );
        Environment.SetEnvironmentVariable("JWT_SECRET", "integration-test-secret-key-value");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "Chatty.Tests");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "Chatty.Tests.Clients");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ChatDbContext>();
            services.RemoveAll<DbContextOptions<ChatDbContext>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<ChatDbContext>>>();
            services.RemoveAll<IPostConfigureOptions<DbContextOptions<ChatDbContext>>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ChatDbContext>>();

            services.AddDbContext<ChatDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }
}
