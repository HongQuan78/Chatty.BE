using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Chatty.BE.Domain.Enums;

namespace Chatty.BE.Domain.Entities
{
    [Table("Messages")]
    public class Message : BaseEntity
    {
        [Required]
        public Guid ConversationId { get; set; }

        [Required]
        public Guid SenderId { get; set; }

        /// <summary>
        /// Nội dung chính của message (text hoặc caption cho image/file).
        /// </summary>
        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = default!;

        [Required]
        public MessageType Type { get; set; }

        /// <summary>
        /// Trạng thái tổng quát (ví dụ: trạng thái phía sender).
        /// </summary>
        [Required]
        public MessageStatus Status { get; set; }

        [ForeignKey(nameof(ConversationId))]
        public virtual Conversation Conversation { get; set; } = default!;

        [ForeignKey(nameof(SenderId))]
        public virtual User Sender { get; set; } = default!;

        // Navigation: attachments (image/file)
        public virtual ICollection<MessageAttachment> Attachments { get; set; } =
            new HashSet<MessageAttachment>();

        // Navigation: receipts per user (delivered/read)
        public virtual ICollection<MessageReceipt> Receipts { get; set; } =
            new HashSet<MessageReceipt>();
    }
}
