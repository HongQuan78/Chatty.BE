namespace Chatty.BE.API.Contracts.Messages
{
    public sealed record SendMessageRequest(
        Guid SenderId,
        string Content,
        Domain.Enums.MessageType Type,
        IEnumerable<Domain.Entities.MessageAttachment>? Attachments
    );
}
