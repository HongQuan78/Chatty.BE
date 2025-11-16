namespace Chatty.BE.Application.DTOs.Messages;

public class MarkConversationReadCommand
{
    public Guid ConversationId { get; set; }
    public Guid ReaderUserId { get; set; }
}
