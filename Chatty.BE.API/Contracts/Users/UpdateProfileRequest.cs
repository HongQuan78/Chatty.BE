namespace Chatty.BE.API.Contracts.Users;

public sealed class UpdateProfileRequest
{
    public string? DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
}
