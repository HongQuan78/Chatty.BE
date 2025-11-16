using Chatty.BE.Domain.Enums;

namespace Chatty.BE.Application.DTOs.MessageReceipts;

public class UpdateReceiptStatusCommand
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public MessageStatus Status { get; set; }
}
