using Chatty.BE.Application.DTOs.Files;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Infrastructure.Config.Upload;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.Infrastructure.Services;

public sealed class CloudinaryFileStorageService(
    CloudinaryOptions options,
    ILogger<CloudinaryFileStorageService> logger) : IFileStorageService
{
    private readonly Cloudinary _client = new(
        new Account(options.CloudName, options.ApiKey, options.ApiSecret)
    );

    public async Task<FileUploadResult> UploadFileAsync(
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var prefix = string.IsNullOrWhiteSpace(options.Folder)
            ? null
            : options.Folder!.Trim().Trim('/');
        
        var publicId = prefix is null ? $"{Guid.NewGuid():N}" : $"{prefix}/{Guid.NewGuid():N}";

        // Note: Using ImageUploadParams as it works for most general files in Cloudinary 
        // unless specific raw/video processing is required.
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, file),
            PublicId = publicId,
            Folder = prefix,
            Overwrite = false,
            UniqueFilename = false,
            UseFilename = false
        };

        var result = await _client.UploadAsync(uploadParams, ct).ConfigureAwait(false);

        if (result.Error != null)
        {
            logger.LogError("Cloudinary upload failed: {Error}", result.Error.Message);
            throw new InvalidOperationException($"File upload failed: {result.Error.Message}");
        }

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
