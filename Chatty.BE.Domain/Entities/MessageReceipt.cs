using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Chatty.BE.Domain.Enums;

namespace Chatty.BE.Domain.Entities
{
    [Table("MessageReceipts")]
    public class MessageReceipt : BaseEntity
    {
        [Required]
        public Guid MessageId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public MessageStatus Status { get; set; }

        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        [ForeignKey(nameof(MessageId))]
        public virtual Message Message { get; set; } = default!;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = default!;
    }
}
