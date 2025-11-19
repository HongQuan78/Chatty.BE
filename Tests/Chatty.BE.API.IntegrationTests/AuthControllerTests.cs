using System.Net.Http.Json;
using Chatty.BE.API.Contracts.Auth;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chatty.BE.API.IntegrationTests;

public class AuthControllerTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory =
        factory.Services.GetRequiredService<IServiceScopeFactory>();

    [Fact]
    public async Task RegisterAsync_PersistsUserAndReturnsPayload()
    {
        var request = new RegisterRequest
        {
            UserName = $"user_{Guid.NewGuid():N}",
            Email = $"user_{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(payload);
        Assert.Equal(request.UserName, payload!.UserName);
        Assert.Equal(request.Email, payload.Email);
        Assert.NotEqual(Guid.Empty, payload.Id);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

        Assert.NotNull(user);
        Assert.Equal(payload.Id, user!.Id);
    }
}
