using System.ComponentModel.DataAnnotations;

namespace Chatty.BE.API.Contracts.Auth;

public sealed record LogoutRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    [MinLength(10)]
    public string RefreshToken { get; init; } = string.Empty;
}
