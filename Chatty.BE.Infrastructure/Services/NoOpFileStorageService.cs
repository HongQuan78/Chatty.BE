using Chatty.BE.Application.DTOs.Files;
using Chatty.BE.Application.Interfaces.Services;

namespace Chatty.BE.Infrastructure.Services;

public sealed class NoOpFileStorageService : IFileStorageService
{
    public Task<FileUploadResult> UploadFileAsync(
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        long bytes = 0;
        if (file.CanSeek)
        {
            bytes = file.Length;
        }

        var result = new FileUploadResult
        {
            PublicId = $"noop-{Guid.NewGuid():N}",
            FileName = fileName,
            SecureUrl = $"https://local-placeholder.invalid/{Uri.EscapeDataString(fileName)}",
            ResourceType = "unknown",
            ContentType = contentType ?? string.Empty,
            Bytes = bytes,
            Width = 0,
            Height = 0,
        };

        return Task.FromResult(result);
    }
}