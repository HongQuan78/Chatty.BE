namespace Chatty.BE.Application.DTOs.Conversations;

public class RemoveParticipantCommand
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
}
