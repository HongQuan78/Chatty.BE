using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chatty.BE.Domain.Entities
{
    [Table("MessageAttachments")]
    public class MessageAttachment : BaseEntity
    {
        [Required]
        public Guid MessageId { get; set; }

        [Required]
        [MaxLength(256)]
        public string FileName { get; set; } = default!;

        [Required]
        [MaxLength(1024)]
        public string FileUrl { get; set; } = default!;

        [MaxLength(256)]
        public string? ContentType { get; set; }

        public long? FileSizeBytes { get; set; }

        [ForeignKey(nameof(MessageId))]
        public virtual Message Message { get; set; } = default!;
    }
}
