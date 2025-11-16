namespace Chatty.BE.Application.DTOs.MessageAttachments;

public class MessageAttachmentDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
}
