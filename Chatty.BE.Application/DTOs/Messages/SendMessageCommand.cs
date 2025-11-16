using Chatty.BE.Application.DTOs.MessageAttachments;
using Chatty.BE.Domain.Enums;

namespace Chatty.BE.Application.DTOs.Messages;

public class SendMessageCommand
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = default!;
    public MessageType Type { get; set; }
    public ICollection<CreateMessageAttachmentRequest>? Attachments { get; set; }
}
