using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chatty.BE.API.Contracts.Auth;
using Chatty.BE.API.Contracts.Conversations;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.DTOs.Conversations;
using Xunit.Abstractions;
using ApiCreateGroupConversationRequest = Chatty.BE.API.Contracts.Conversations.CreateGroupConversationRequest;
using ApiCreatePrivateConversationRequest = Chatty.BE.API.Contracts.Conversations.CreatePrivateConversationRequest;

namespace Chatty.BE.API.IntegrationTests;

public class ConversationsControllerTests(AuthApiFactory factory, ITestOutputHelper output)
    : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task CreatePrivateConversation_ReturnsDtoAndCanBeFetched()
    {
        var (userA, passwordA) = await RegisterUserAsync();
        var (userB, _) = await RegisterUserAsync();
        var accessToken = await LoginAsync(userA.Email, passwordA);

        var createResponse = await SendAuthorizedAsync(
            accessToken,
            HttpMethod.Post,
            "/api/conversations/private",
            new ApiCreatePrivateConversationRequest { UserAId = userA.Id, UserBId = userB.Id }
        );
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();
        Assert.NotNull(created);
        Assert.False(created!.IsGroup);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var fetchResponse = await SendAuthorizedAsync(
            accessToken,
            HttpMethod.Get,
            $"/api/conversations/{created.Id}"
        );
        fetchResponse.EnsureSuccessStatusCode();
        var fetched = await fetchResponse.Content.ReadFromJsonAsync<ConversationDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task AddParticipant_AddsUserToGroupConversation()
    {
        var (owner, ownerPassword) = await RegisterUserAsync();
        var (memberA, _) = await RegisterUserAsync();
        var (memberB, _) = await RegisterUserAsync();
        var ownerToken = await LoginAsync(owner.Email, ownerPassword);

        var createResponse = await SendAuthorizedAsync(
            ownerToken,
            HttpMethod.Post,
            "/api/conversations/group",
            new ApiCreateGroupConversationRequest
            {
                OwnerId = owner.Id,
                Name = "Test Group",
                ParticipantIds = new List<Guid> { memberA.Id },
            }
        );
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();
        Assert.NotNull(created);

        var addResponse = await SendAuthorizedAsync(
            ownerToken,
            HttpMethod.Post,
            $"/api/conversations/{created!.Id}/participants",
            new AddParticipantRequest(memberB.Id)
        );
        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        var fetchResponse = await SendAuthorizedAsync(
            ownerToken,
            HttpMethod.Get,
            $"/api/conversations/{created.Id}"
        );
        fetchResponse.EnsureSuccessStatusCode();
        var fetched = await fetchResponse.Content.ReadFromJsonAsync<ConversationDto>();
        Assert.NotNull(fetched);
        Assert.Equal(3, fetched!.Participants?.Count); // owner + memberA + memberB
    }

    private async Task<(RegisterResponse User, string Password)> RegisterUserAsync()
    {
        var unique = Guid.NewGuid().ToString("N");
        var password = "Password123!";
        var request = new RegisterRequest
        {
            UserName = $"user_{unique}",
            Email = $"user_{unique}@example.com",
            Password = password,
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(payload);
        return (payload!, password);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(email, password)
        );
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(payload);
        return payload!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        string accessToken,
        HttpMethod method,
        string url,
        object? body = null
    )
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Request to {url} failed: {response.StatusCode} - {content}");
        }
        return response;
    }
}
