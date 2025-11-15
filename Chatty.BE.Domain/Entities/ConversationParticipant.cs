using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chatty.BE.Domain.Entities
{
    [Table("ConversationParticipants")]
    public class ConversationParticipant : BaseEntity
    {
        [Required]
        public Guid ConversationId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public bool IsAdmin { get; set; }

        [Required]
        public DateTime JoinedAt { get; set; }

        [ForeignKey(nameof(ConversationId))]
        public virtual Conversation Conversation { get; set; } = default!;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = default!;
    }
}
