using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.Infrastructure.SignalR;

[Authorize]
public sealed class ChatHub(ILogger<ChatHub> logger) : Hub<IChatClient>
{
    private readonly ILogger<ChatHub> _logger = logger;

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is null)
        {
            _logger.LogWarning("Connection rejected: missing or invalid user id.");
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    private string? GetUserId()
    {
        var idValue =
            Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(idValue, out var parsed) ? parsed.ToString() : null;
    }
}
