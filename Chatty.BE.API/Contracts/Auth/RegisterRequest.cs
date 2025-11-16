using System.ComponentModel.DataAnnotations;

namespace Chatty.BE.API.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required]
    [MaxLength(50)]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
