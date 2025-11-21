using Chatty.BE.Application.DTOs.Files;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Config.Upload;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Chatty.BE.Infrastructure.Services;

public sealed class CloudinaryFileStorageService(CloudinaryOptions options) : IFileStorageService
{
    private readonly Cloudinary _client = new Cloudinary(
        new Account(options.CloudName, options.ApiKey, options.ApiSecret)
    );
    private readonly CloudinaryOptions _options =
        options ?? throw new ArgumentNullException(nameof(options));

    public async Task<FileUploadResult> UploadFileAsync(
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var prefix = string.IsNullOrWhiteSpace(_options.Folder)
            ? null
            : _options.Folder!.Trim().Trim('/');
        var publicId = prefix is null ? $"{Guid.NewGuid():N}" : $"{prefix}/{Guid.NewGuid():N}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, file),
            PublicId = publicId,
            UseFilename = false,
            UniqueFilename = false,
            Overwrite = false,
            Folder = prefix,
        };

        var result = await _client.UploadAsync(uploadParams, ct).ConfigureAwait(false);

        return new FileUploadResult
        {
            PublicId = result.PublicId,
            FileName = result.OriginalFilename ?? fileName,
            SecureUrl = result.SecureUrl?.ToString() ?? string.Empty,
            ResourceType = result.ResourceType,
            ContentType = contentType,
            Bytes = result.Bytes,
            Width = result.Width,
            Height = result.Height,
        };
    }
}
