namespace Chatty.BE.API.Contracts.Files;

public sealed record class UploadFileRequest
{
    public string FileUrl { get; init; } = null!;
}

public sealed class UploadFileForm
{
    public required IFormFile File { get; init; }
}
