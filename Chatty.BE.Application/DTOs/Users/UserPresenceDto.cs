namespace Chatty.BE.Application.DTOs.Users;

public sealed class UserPresenceDto
{
    public Guid UserId { get; init; }
    public bool IsOnline { get; init; }
    public DateTime? LastActiveUtc { get; init; }
    public int? OfflineMinutes { get; init; }
}
