namespace Chatty.BE.Application.DTOs.Auth;

public class RegisterRequest
{
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}
