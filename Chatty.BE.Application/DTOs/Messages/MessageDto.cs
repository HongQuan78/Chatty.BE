using Chatty.BE.Application.DTOs.MessageAttachments;
using Chatty.BE.Application.DTOs.MessageReceipts;
using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Domain.Enums;

namespace Chatty.BE.Application.DTOs.Messages;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = default!;
    public MessageType Type { get; set; }
    public MessageStatus Status { get; set; }
    public UserDto? Sender { get; set; }
    public IReadOnlyList<MessageAttachmentDto>? Attachments { get; set; }
    public IReadOnlyList<MessageReceiptDto>? Receipts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
