namespace Chatty.BE.Application.DTOs.Conversations;

public class AddParticipantCommand
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
}
