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
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded." });

            long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxFileSize)
                return BadRequest(new { success = false, message = "File exceeds the 10 MB limit." });

            var uniqueCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            // 3. Delegate the saving logic to the Storage Service!
            string savedPath = await _storage.SaveFileAsync(file, uniqueCode);

            var metadata = new FileMetadata
            {
                Code = uniqueCode,
                OriginalFileName = file.FileName,
                MimeType = file.ContentType,
                SizeBytes = file.Length,
                StoragePath = savedPath, // Use the path returned by the service
                MaxDownloads = 10,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _repo.AddAsync(metadata);

            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                data = metadata
            });
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

            if (!System.IO.File.Exists(metadata.StoragePath))
                return NotFound("The physical file is missing from the server.");

            metadata.DownloadCount++;
            await _repo.UpdateAsync(metadata);

            var stream = new FileStream(metadata.StoragePath, FileMode.Open, FileAccess.Read);
            return File(stream, metadata.MimeType, metadata.OriginalFileName);
        }
    }
}