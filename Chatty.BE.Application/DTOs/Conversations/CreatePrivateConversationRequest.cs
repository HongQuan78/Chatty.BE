namespace Chatty.BE.Application.DTOs.Conversations;

public class CreatePrivateConversationRequest
{
    public Guid UserAId { get; set; }
    public Guid UserBId { get; set; }
}
