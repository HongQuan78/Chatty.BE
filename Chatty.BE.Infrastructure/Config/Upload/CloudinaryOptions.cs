namespace Chatty.BE.Infrastructure.Config.Upload;

public sealed record class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string? Folder { get; set; }
}
