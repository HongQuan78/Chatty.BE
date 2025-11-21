namespace Chatty.BE.Application.Common.Helpers;

public static class FileValidationService
{
    private static readonly HashSet<string> AllowedImageExtensions = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".tiff",
        ".webp",
    };

    public static bool IsValidImageFileExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrWhiteSpace(extension) && AllowedImageExtensions.Contains(extension);
    }
}
