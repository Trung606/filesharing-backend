using Microsoft.AspNetCore.Mvc;
using FileSharingAPI.Models;
using FileSharingAPI.Repositories;

namespace FileSharingAPI.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly IFileRepository _repo;

        public FilesController(IFileRepository repo)
        {
            _repo = repo;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            // 1. Validate the file exists and enforce the 10 MB limit
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxFileSize)
                return BadRequest("File exceeds the 10 MB limit.");

            // 2. Generate a random 6-character short code
            var uniqueCode = Guid.NewGuid().ToString("N").Substring(0, 6);

            // 3. Save the physical file to the local wwwroot/uploads directory
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            var filePath = Path.Combine(uploadDirectory, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 4. Save the metadata to the PostgreSQL database
            var metadata = new FileMetadata
            {
                Code = uniqueCode,
                OriginalFileName = file.FileName,
                MimeType = file.ContentType,
                SizeBytes = file.Length,
                StoragePath = filePath,
                MaxDownloads = 10, // Default allowed downloads
                ExpiresAt = DateTime.UtcNow.AddDays(7) // Default 7-day expiration
            };

            await _repo.AddAsync(metadata);

            // 5. Return a 201 Created response containing the short code
            return StatusCode(StatusCodes.Status201Created, new { code = uniqueCode, metadata });
        }
        [HttpGet("{code}")]
        public async Task<IActionResult> DownloadFile(string code)
        {
            // 1. Fetch metadata from the database
            var metadata = await _repo.GetByCodeAsync(code);
            if (metadata == null)
                return NotFound("File not found.");

            // 2. Enforce Expiration Date Rule
            if (metadata.ExpiresAt.HasValue && DateTime.UtcNow > metadata.ExpiresAt.Value)
                return StatusCode(StatusCodes.Status410Gone, "This link has expired.");

            // 3. Enforce Max Downloads Rule
            if (metadata.DownloadCount >= metadata.MaxDownloads)
                return StatusCode(StatusCodes.Status410Gone, "Maximum download limit reached.");

            // 4. Ensure the physical file actually exists on the disk
            if (!System.IO.File.Exists(metadata.StoragePath))
                return NotFound("The physical file is missing from the server.");

            // 5. Increment the download counter and save to the database
            metadata.DownloadCount++;
            await _repo.UpdateAsync(metadata);

            // 6. Read the file stream and return it to the client
            var stream = new FileStream(metadata.StoragePath, FileMode.Open, FileAccess.Read);
            return File(stream, metadata.MimeType, metadata.OriginalFileName);
        }
    }
}