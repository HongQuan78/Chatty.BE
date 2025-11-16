namespace Chatty.BE.Application.DTOs.Messages;

public class GetMessagesQuery
{
    public Guid ConversationId { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
