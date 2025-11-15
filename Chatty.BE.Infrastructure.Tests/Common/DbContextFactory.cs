using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chatty.BE.Infrastructure.Tests.Common;

public static class DbContextFactory
{
    public static ChatDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ChatDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
