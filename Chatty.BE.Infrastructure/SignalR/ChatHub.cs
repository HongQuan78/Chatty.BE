using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.Infrastructure.SignalR;

[Authorize]
public sealed class ChatHub(ILogger<ChatHub> logger, IPresenceService presenceService)
    : Hub<IChatClient>
{
    private readonly ILogger<ChatHub> _logger = logger;
    private readonly IPresenceService _presenceService = presenceService;

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is null)
        {
            _logger.LogWarning("Connection rejected: missing or invalid user id.");
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());
        await _presenceService.UpdateLastActiveAsync(userId.Value, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            await _presenceService.UpdateLastActiveAsync(userId.Value, Context.ConnectionAborted);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public Task Heartbeat(CancellationToken ct = default)
    {
        var userId = GetUserId();
        return userId is null
            ? Task.CompletedTask
            : _presenceService.UpdateLastActiveAsync(userId.Value, ct);
    }

    private Guid? GetUserId()
    {
        var idValue =
            Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(idValue, out var parsed) ? parsed : null;
    }
}
