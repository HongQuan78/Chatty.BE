namespace Chatty.BE.Application.DTOs.Users;

public class UpdateUserProfileRequest
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
}
