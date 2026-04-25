using System.Security.Claims;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Chatty.BE.Infrastructure.SignalR;

/// <summary>
/// Central hub for real-time chat interactions, managing presence and group subscriptions.
/// </summary>
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
            logger.LogWarning("Connection {ConnectionId} rejected: missing or invalid user id.", Context.ConnectionId);
            Context.Abort();
            return;
        }

        try
        {
            // Add to individual user group for targeted notifications (e.g., "New Message", "Friend Request")
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId.Value));

            await presenceService.UpdateLastActiveAsync(userId.Value, Context.ConnectionAborted);

            logger.LogInformation("User {UserId} connected with ConnectionId {ConnectionId}", userId, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during OnConnectedAsync for user {UserId}", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            try
            {
                await presenceService.UpdateLastActiveAsync(userId.Value, Context.ConnectionAborted);
                logger.LogInformation("User {UserId} disconnected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during OnDisconnectedAsync for user {UserId}", userId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Joins a specific conversation group to receive real-time updates (messages, read receipts).
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    public async Task JoinConversation(Guid conversationId)
    {
        if (conversationId == Guid.Empty)
        {
            throw new HubException("Invalid conversation ID.");
        }

        var userId = GetUserId() ?? throw new HubException("User not authenticated.");

        try
        {
            // Security check: Verify the user is actually a participant in this conversation
            var isInResult = await conversationService.UserIsInConversationAsync(conversationId, userId);

            if (isInResult.IsSuccess && isInResult.Value)
            {
                var groupName = GetConversationGroup(conversationId);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                // Notify others in the conversation that a user is now active in this chat context
                await Clients.Group(groupName).UserJoinedConversation(conversationId, userId);

                logger.LogDebug("User {UserId} joined conversation group {ConversationId}", userId, conversationId);
            }
            else
            {
                logger.LogWarning("Unauthorized join attempt: User {UserId} -> Conversation {ConversationId}", userId, conversationId);
                throw new HubException("You are not a participant in this conversation.");
            }
        }
        catch (HubException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in JoinConversation for User {UserId}, Conversation {ConversationId}", userId, conversationId);
            throw new HubException("An error occurred while joining the conversation.");
        }
    }

    /// <summary>
    /// Leaves a specific conversation group.
    /// </summary>
    public async Task LeaveConversation(Guid conversationId)
    {
        if (conversationId == Guid.Empty) return;

        var userId = GetUserId();
        var groupName = GetConversationGroup(conversationId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        if (userId.HasValue)
        {
            await Clients.Group(groupName).UserLeftConversation(conversationId, userId.Value);
        }
    }

    /// <summary>
    /// Periodically called by the client to keep presence status alive.
    /// </summary>
    public async Task Heartbeat(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return;

        try
        {
            await presenceService.UpdateLastActiveAsync(userId.Value, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Heartbeat failed for user {UserId}", userId);
        }
    }

    private static string GetUserGroup(Guid userId) => userId.ToString();
    private static string GetConversationGroup(Guid conversationId) => conversationId.ToString();

    private Guid? GetUserId()
    {
        var idValue =
            Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(idValue, out var parsed) ? parsed : null;
    }
}
