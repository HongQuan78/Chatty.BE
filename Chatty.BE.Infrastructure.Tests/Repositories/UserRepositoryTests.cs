using Chatty.BE.Domain.Entities;
using Chatty.BE.Infrastructure.Repositories;
using Chatty.BE.Infrastructure.Tests.Common;
using Xunit;

namespace Chatty.BE.Infrastructure.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task AddUser_ShouldInsertIntoDatabase()
    {
        // Arrange
        var context = DbContextFactory.Create();
        var repo = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash"
        };

        // Act
        await repo.AddAsync(user);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Users.FindAsync(user.Id);
        Assert.NotNull(saved);
        Assert.Equal("testuser", saved!.UserName);
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnUser()
    {
        var context = DbContextFactory.Create();
        var repo = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "user2",
            Email = "user2@example.com",
            PasswordHash = "hash"
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var found = await repo.GetByEmailAsync("user2@example.com");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }
}
