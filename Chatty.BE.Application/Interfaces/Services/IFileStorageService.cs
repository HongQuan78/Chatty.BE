namespace Chatty.BE.Application.Interfaces.Services
{
    public interface IFileStorageService
    {
        Task<DTOs.Files.FileUploadResult> UploadFileAsync(
            Stream file,
            string fileName,
            string contentType,
            CancellationToken ct
        );
    }
}
