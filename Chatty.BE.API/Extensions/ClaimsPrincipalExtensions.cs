// Chatty.BE.API/Extensions/ClaimsPrincipalExtensions.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Chatty.BE.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var idValue =
            user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(idValue, out var id))
        {
            return id;
        }

        throw new InvalidOperationException("Authenticated user id is missing or invalid.");
    }
}
