using Microsoft.AspNetCore.Mvc;
using FileSharingAPI.Models;
using FileSharingAPI.Repositories;
using FileSharingAPI.Services; // Make sure this is here!

namespace FileSharingAPI.Controllers
{
    [ApiController]
    [Route("api/v1/files")]
    public class FilesController : ControllerBase
    {
        private readonly IFileRepository _repo;
        private readonly IStorageService _storage; // 1. Add the storage service

        // 2. Inject both the repository and the storage service into the constructor
        public FilesController(IFileRepository repo, IStorageService storage)
        {
            _repo = repo;
            _storage = storage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(
            IFormFile file,
            [FromForm] int? maxDownloads = null,   // <-- Add = null default value
            [FromForm] int? expiryHours = null)    // <-- Add = null default value
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded." });

            long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxFileSize)
                return BadRequest(new { success = false, message = "File exceeds the 10 MB limit." });

            var uniqueCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            string savedPath = await _storage.SaveFileAsync(file, uniqueCode);

            var metadata = new FileMetadata
            {
                Code = uniqueCode,
                OriginalFileName = file.FileName,
                MimeType = file.ContentType,
                SizeBytes = file.Length,
                StoragePath = savedPath,
                MaxDownloads = maxDownloads ?? 100,
                ExpiresAt = expiryHours.HasValue ? DateTime.UtcNow.AddHours(expiryHours.Value) : null
            };

            await _repo.AddAsync(metadata);

            return StatusCode(StatusCodes.Status201Created, new { success = true, data = metadata });
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetFileInfo(string code)
        {
            var metadata = await _repo.GetByCodeAsync(code);
            if (metadata == null)
                return NotFound(new { success = false, message = "File not found." });

            if (metadata.ExpiresAt.HasValue && DateTime.UtcNow > metadata.ExpiresAt.Value)
                return StatusCode(StatusCodes.Status410Gone, new { success = false, message = "Link expired." });

            if (metadata.DownloadCount >= metadata.MaxDownloads)
                return StatusCode(StatusCodes.Status410Gone, new { success = false, message = "Download limit reached." });

            return Ok(new { success = true, data = metadata });
        }

        [HttpGet("download/{code}")]
        public async Task<IActionResult> DownloadFile(string code)
        {
            var metadata = await _repo.GetByCodeAsync(code);

            if (metadata == null)
                return NotFound("File not found.");

            if (metadata.ExpiresAt.HasValue && DateTime.UtcNow > metadata.ExpiresAt.Value)
                return StatusCode(StatusCodes.Status410Gone, "This link has expired.");

            if (metadata.DownloadCount >= metadata.MaxDownloads)
                return StatusCode(StatusCodes.Status410Gone, "Maximum download limit reached.");

            metadata.DownloadCount++;
            await _repo.UpdateAsync(metadata);

            // Redirect the user directly to the secure Cloudinary URL
            return Redirect(metadata.StoragePath);
        }
    }
}