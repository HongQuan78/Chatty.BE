namespace Chatty.BE.Application.DTOs.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActive { get; set; }
    public DateTime? LatestLogin { get; set; }
    public DateTime? LatestLogout { get; set; }
}
