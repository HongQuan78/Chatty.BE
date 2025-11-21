using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chatty.BE.API.Contracts.Auth;
using ApiCreatePrivateConversationRequest = Chatty.BE.API.Contracts.Conversations.CreatePrivateConversationRequest;
using ApiSendMessageRequest = Chatty.BE.API.Contracts.Messages.SendMessageRequest;
using Chatty.BE.API.Contracts.Conversations;
using Chatty.BE.API.Contracts.Messages;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.DTOs.Conversations;
using Chatty.BE.Application.DTOs.Messages;
using Chatty.BE.Domain.Enums;

namespace Chatty.BE.API.IntegrationTests;

public class MessagesControllerTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SendMessage_ReadFlow_WorksForParticipants()
    {
        var (userA, pwdA) = await RegisterUserAsync();
        var (userB, pwdB) = await RegisterUserAsync();
        var tokenA = await LoginAsync(userA.Email, pwdA);
        var tokenB = await LoginAsync(userB.Email, pwdB);

        var conversation = await CreatePrivateConversationAsync(tokenA, userA.Id, userB.Id);

        var sendResponse = await SendAuthorizedAsync(
            tokenA,
            HttpMethod.Post,
            $"/api/conversations/{conversation.Id}/messages",
            new ApiSendMessageRequest(userA.Id, "Hello world", MessageType.Text, null)
        );
        sendResponse.EnsureSuccessStatusCode();
        var sent = await sendResponse.Content.ReadFromJsonAsync<MessageDto>();
        Assert.NotNull(sent);
        Assert.Equal(conversation.Id, sent!.ConversationId);
        Assert.Equal(userA.Id, sent.SenderId);

        var listResponse = await SendAuthorizedAsync(
            tokenA,
            HttpMethod.Get,
            $"/api/conversations/{conversation.Id}/messages?page=1&pageSize=10"
        );
        listResponse.EnsureSuccessStatusCode();
        var messages = await listResponse.Content.ReadFromJsonAsync<List<MessageDto>>();
        Assert.NotNull(messages);
        Assert.Contains(messages!, m => m.Id == sent.Id && m.Content == "Hello world");

        var unreadCountBefore = await GetUnreadCountAsync(tokenB, conversation.Id);
        Assert.Equal(1, unreadCountBefore);

        var markReadResponse = await SendAuthorizedAsync(
            tokenB,
            HttpMethod.Put,
            $"/api/conversations/{conversation.Id}/messages/read"
        );
        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);

        var unreadCountAfter = await GetUnreadCountAsync(tokenB, conversation.Id);
        Assert.Equal(0, unreadCountAfter);
    }

    private async Task<ConversationDto> CreatePrivateConversationAsync(
        string accessToken,
        Guid userAId,
        Guid userBId
    )
    {
        var response = await SendAuthorizedAsync(
            accessToken,
            HttpMethod.Post,
            "/api/conversations/private",
            new ApiCreatePrivateConversationRequest { UserAId = userAId, UserBId = userBId }
        );
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ConversationDto>();
        return dto!;
    }

    private async Task<int> GetUnreadCountAsync(string accessToken, Guid conversationId)
    {
        var response = await SendAuthorizedAsync(
            accessToken,
            HttpMethod.Get,
            $"/api/conversations/{conversationId}/messages/unread-count"
        );
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<UnreadCountResponse>();
        return payload!.Count;
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
        return await _client.SendAsync(request);
    }

    private sealed record UnreadCountResponse(int Count);
}
