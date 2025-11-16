namespace Chatty.BE.Application.DTOs.Conversations;

public class CreateGroupConversationRequest
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = default!;
    public ICollection<Guid> ParticipantIds { get; set; } = new List<Guid>();
}
