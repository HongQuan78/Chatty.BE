using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chatty.BE.Domain.Entities
{
    [Table("Conversations")]
    public class Conversation : BaseEntity
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [Required]
        public bool IsGroup { get; set; }

        // Optional: owner for group conversations
        public Guid? OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public virtual User? Owner { get; set; }

        // Navigation: participants (N-N với User)
        public virtual ICollection<ConversationParticipant> Participants { get; set; } =
            new HashSet<ConversationParticipant>();

        // Navigation: messages
        public virtual ICollection<Message> Messages { get; set; } = new HashSet<Message>();
    }
}
