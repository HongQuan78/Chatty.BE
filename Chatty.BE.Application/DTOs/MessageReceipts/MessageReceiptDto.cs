using Chatty.BE.Application.DTOs.Users;
using Chatty.BE.Domain.Enums;

namespace Chatty.BE.Application.DTOs.MessageReceipts;

public class MessageReceiptDto
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public MessageStatus Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public UserDto? User { get; set; }
}
