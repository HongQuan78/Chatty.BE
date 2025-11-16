using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.DTOs.ConversationParticipants;

public class ConversationParticipantDto
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; }
    public UserDto? User { get; set; }
}
