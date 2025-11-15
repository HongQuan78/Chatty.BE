using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Chatty.BE.Domain.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } = default!;

        [Required]
        [MaxLength(256)]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MaxLength(512)]
        public string PasswordHash { get; set; } = default!;

        [MaxLength(100)]
        public string? DisplayName { get; set; }

        [MaxLength(512)]
        public string? AvatarUrl { get; set; }

        [MaxLength(512)]
        public string? Bio { get; set; }

        // Navigation: conversations that user participates in
        public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } =
            new HashSet<ConversationParticipant>();

        // Navigation: messages sent by this user
        public virtual ICollection<Message> MessagesSent { get; set; } = new HashSet<Message>();

        // Navigation: per-message receipts (delivered/read)
        public virtual ICollection<MessageReceipt> MessageReceipts { get; set; } =
            new HashSet<MessageReceipt>();
    }
}
