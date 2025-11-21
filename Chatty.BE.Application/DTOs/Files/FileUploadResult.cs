namespace Chatty.BE.Application.DTOs.Files;

public sealed class FileUploadResult
{
    public string PublicId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Bytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
