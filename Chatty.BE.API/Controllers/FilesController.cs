using Chatty.BE.API.Contracts.Files;
using Chatty.BE.Application.Common.Helpers;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController(IFileStorageService fileStorageService) : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService = fileStorageService;

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<IActionResult> UploadFile(
            [FromForm] UploadFileForm request,
            CancellationToken ct = default
        )
        {
            if (request.File is null || request.File.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            const long maxBytes = 10 * 1024 * 1024; // 10 MB
            if (request.File.Length > maxBytes)
            {
                return BadRequest("File is too large. Max 10 MB.");
            }

            if (!FileValidationService.IsValidImageFileExtension(request.File.FileName))
            {
                return BadRequest("Unsupported file type.");
            }

            using var stream = request.File.OpenReadStream();
            var result = await _fileStorageService.UploadFileAsync(
                stream,
                request.File.FileName,
                request.File.ContentType,
                ct
            );

            return Ok(new UploadFileRequest { FileUrl = result.SecureUrl });
        }
    }
}
