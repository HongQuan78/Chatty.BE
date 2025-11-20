namespace Chatty.BE.API.Contracts.Conversations
{
    public sealed record CreatePrivateConversationRequest
    {
        public Guid UserAId { get; init; }
        public Guid UserBId { get; init; }
    }
}
