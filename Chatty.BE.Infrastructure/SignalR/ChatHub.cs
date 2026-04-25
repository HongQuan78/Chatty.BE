using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.Infrastructure.SignalR;

[Authorize]
public sealed class ChatHub(
    ILogger<ChatHub> logger,
    IPresenceService presenceService,
    IConversationService conversationService)
    : Hub<IChatClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is null)
        {
            logger.LogWarning("Connection rejected: missing or invalid user id.");
            Context.Abort();
            return;
        }

        // Add to individual user group for targeted notifications (e.g., "New Message")
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());

        await presenceService.UpdateLastActiveAsync(userId.Value, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            await presenceService.UpdateLastActiveAsync(userId.Value, Context.ConnectionAborted);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Explicitly join a conversation group to receive real-time updates for that conversation.
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId is null) return;

        // Security check: Verify the user is actually a participant in this conversation
        var isIn = await conversationService.UserIsInConversationAsync(conversationId, userId.Value);
        if (isIn.IsSuccess && isIn.Value)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
            logger.LogDebug("User {UserId} joined conversation group {ConversationId}", userId, conversationId);
        }
        else
        {
            logger.LogWarning("User {UserId} attempted to join unauthorized conversation {ConversationId}", userId, conversationId);
        }
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public Task Heartbeat(CancellationToken ct = default)
    {
        var userId = GetUserId();
        return userId is null
            ? Task.CompletedTask
            : presenceService.UpdateLastActiveAsync(userId.Value, ct);
    }

    private Guid? GetUserId()
    {
        var idValue =
            Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(idValue, out var parsed) ? parsed : null;
    }
}
