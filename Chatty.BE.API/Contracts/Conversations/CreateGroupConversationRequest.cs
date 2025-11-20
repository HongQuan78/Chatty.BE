namespace Chatty.BE.API.Contracts.Conversations
{
    public sealed record CreateGroupConversationRequest
    {
        public Guid OwnerId { get; init; }

        public string Name { get; init; } = string.Empty;
        public List<Guid> ParticipantIds { get; init; } = [];
    }
}
