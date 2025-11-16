namespace Chatty.BE.Application.DTOs.MessageAttachments;

public class CreateMessageAttachmentRequest
{
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
}
