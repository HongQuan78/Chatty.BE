using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chatty.BE.API.Contracts.Auth;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Chatty.BE.API.IntegrationTests;

public class AuthControllerTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory =
        factory.Services.GetRequiredService<IServiceScopeFactory>();

    [Fact]
    public async Task RegisterAsync_PersistsUserAndReturnsPayload()
    {
        var request = BuildRegisterRequest();

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

    [Fact]
    public async Task LoginAsync_ReturnsAccessAndRefreshTokens()
    {
        var session = await RegisterAndLoginAsync();

        Assert.NotEqual(Guid.Empty, session.Login.UserId);
        Assert.False(string.IsNullOrWhiteSpace(session.Login.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(session.Login.RefreshToken));
    }

    [Fact]
    public async Task RefreshAsync_IssuesNewTokens()
    {
        var session = await RegisterAndLoginAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequestDto(session.Login.RefreshToken)
        );

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
        Assert.NotNull(payload);
        Assert.NotEqual(session.Login.RefreshToken, payload!.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
    }

    [Fact]
    public async Task ChangePasswordAsync_AllowsLoginWithNewPassword()
    {
        var session = await RegisterAndLoginAsync();
        var newPassword = "NewPassword123!";

        var changePasswordRequest = new ChangePasswordRequest
        {
            UserId = session.Login.UserId,
            CurrentPassword = session.Password,
            NewPassword = newPassword,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(changePasswordRequest),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session.Login.AccessToken
        );

        var changeResponse = await _client.SendAsync(request);
        if (changeResponse.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await changeResponse.Content.ReadAsStringAsync();
            throw new XunitException(
                $"Change password failed with {changeResponse.StatusCode}: {body}"
            );
        }

        var newLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(session.Register.Email, newPassword)
        );
        newLoginResponse.EnsureSuccessStatusCode();

        var oldLoginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(session.Register.Email, session.Password)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);
    }

    [Fact]
    public async Task LogoutAsync_RevokesRefreshToken()
    {
        var session = await RegisterAndLoginAsync();

        var logoutResponse = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            new LogoutRequest
            {
                UserId = session.Login.UserId,
                RefreshToken = session.Login.RefreshToken,
            }
        );

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequestDto(session.Login.RefreshToken)
        );

        Assert.Equal(HttpStatusCode.BadRequest, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task LogoutAllSessionsAsync_RevokesAllTokensForUser()
    {
        var session = await RegisterAndLoginAsync();
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequestDto(session.Login.RefreshToken)
        );
        refreshResponse.EnsureSuccessStatusCode();
        var newTokens = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
        Assert.NotNull(newTokens);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout-all-sessions")
        {
            Content = JsonContent.Create(
                new LogoutAllSessionsRequest { UserId = session.Login.UserId }
            ),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session.Login.AccessToken
        );

        var logoutAllResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, logoutAllResponse.StatusCode);

        var firstRefreshAttempt = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequestDto(session.Login.RefreshToken)
        );
        Assert.Equal(HttpStatusCode.BadRequest, firstRefreshAttempt.StatusCode);

        var secondRefreshAttempt = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequestDto(newTokens!.RefreshToken)
        );
        Assert.Equal(HttpStatusCode.BadRequest, secondRefreshAttempt.StatusCode);
    }

    [Fact]
    public async Task LogoutAllSessionsAsync_ReturnsForbidWhenUserIdDoesNotMatchToken()
    {
        var session = await RegisterAndLoginAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout-all-sessions")
        {
            Content = JsonContent.Create(new LogoutAllSessionsRequest { UserId = Guid.NewGuid() }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session.Login.AccessToken
        );

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static RegisterRequest BuildRegisterRequest(string? password = null)
    {
        var unique = Guid.NewGuid().ToString("N");
        return new RegisterRequest
        {
            UserName = $"user_{unique}",
            Email = $"user_{unique}@example.com",
            Password = password ?? "Password123!",
        };
    }

    private async Task<AuthSession> RegisterAndLoginAsync(string? password = null)
    {
        var registerRequest = BuildRegisterRequest(password);
        var registerResponse = await PostRegisterAsync(registerRequest);
        var loginResponse = await PostLoginAsync(
            new LoginRequestDto(registerRequest.Email, registerRequest.Password)
        );

        return new AuthSession(
            registerRequest,
            registerResponse,
            loginResponse,
            registerRequest.Password
        );
    }

    private async Task<RegisterResponse> PostRegisterAsync(RegisterRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    private async Task<LoginResponseDto> PostLoginAsync(LoginRequestDto request)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponseDto>())!;
    }

    private sealed record AuthSession(
        RegisterRequest Register,
        RegisterResponse RegisterResponse,
        LoginResponseDto Login,
        string Password
    );
}
