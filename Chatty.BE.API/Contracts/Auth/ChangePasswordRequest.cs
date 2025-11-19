using System.ComponentModel.DataAnnotations;

namespace Chatty.BE.API.Contracts.Auth;

public sealed record ChangePasswordRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    [MinLength(8)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}
