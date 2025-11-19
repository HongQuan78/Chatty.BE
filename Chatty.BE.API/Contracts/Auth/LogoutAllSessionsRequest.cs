using System.ComponentModel.DataAnnotations;

namespace Chatty.BE.API.Contracts.Auth;

public sealed record LogoutAllSessionsRequest
{
    [Required]
    public Guid UserId { get; init; }
}
