using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FileSharingAPI.Controllers;
using FileSharingAPI.Repositories;
using FileSharingAPI.Services;
using FileSharingAPI.Models;
using System.Threading.Tasks;
using System;

namespace FileSharingAPI.Tests
{
    public class FilesControllerTests
    {
        private readonly Mock<IFileRepository> _mockRepo;
        private readonly Mock<IStorageService> _mockStorage;
        private readonly FilesController _controller;

        public FilesControllerTests()
        {
            // This runs before every single test
            _mockRepo = new Mock<IFileRepository>();
            _mockStorage = new Mock<IStorageService>();
            _controller = new FilesController(_mockRepo.Object, _mockStorage.Object);
        }

        [Fact]
        public async Task UploadFile_FileExceeds10MB_ReturnsBadRequest()
        {
            long elevenMegabytes = 11 * 1024 * 1024;
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(elevenMegabytes);

            var result = await _controller.UploadFile(mockFile.Object);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("File exceeds the 10 MB limit", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task UploadFile_ValidFile_ReturnsCreatedResponse()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024); // 1 KB
            mockFile.Setup(f => f.FileName).Returns("test_image.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            // Fake the storage service so it returns a fake path
            _mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                        .ReturnsAsync("C:\\fake\\path\\test_image.jpg");

            var result = await _controller.UploadFile(mockFile.Object);

            var createdResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, createdResult.StatusCode);

            // Verify the repository's AddAsync method was actually called exactly once
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<FileMetadata>()), Times.Once);
        }

        [Fact]
        public async Task GetFileInfo_FileNotFound_ReturnsNotFound()
        {
            // Tell the fake repo to return null when asked for a file
            _mockRepo.Setup(r => r.GetByCodeAsync("FAKECD")).ReturnsAsync((FileMetadata)null);

            var result = await _controller.GetFileInfo("FAKECD");

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("File not found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task GetFileInfo_ValidCode_ReturnsOk()
        {
            var validFile = new FileMetadata
            {
                Code = "VALID1",
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                DownloadCount = 0,
                MaxDownloads = 10
            };

            _mockRepo.Setup(r => r.GetByCodeAsync("VALID1")).ReturnsAsync(validFile);

            var result = await _controller.GetFileInfo("VALID1");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DownloadFile_DownloadLimitReached_ReturnsGone()
        {
            var limitReachedFile = new FileMetadata
            {
                Code = "MAXOUT",
                DownloadCount = 10,
                MaxDownloads = 10
            };

            _mockRepo.Setup(r => r.GetByCodeAsync("MAXOUT")).ReturnsAsync(limitReachedFile);

            var result = await _controller.DownloadFile("MAXOUT");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(410, objectResult.StatusCode); // 410 Gone
            Assert.Contains("Maximum download limit reached", objectResult.Value.ToString());
        }
        // --- MISSING UPLOAD TEST ---
        [Fact]
        public async Task UploadFile_EmptyFile_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0); // Simulate empty file

            var result = await _controller.UploadFile(mockFile.Object);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("No file uploaded", badRequestResult.Value.ToString());
        }

        // --- MISSING GET FILE INFO TESTS ---
        [Fact]
        public async Task GetFileInfo_FileExpired_ReturnsGone()
        {
            var expiredFile = new FileMetadata
            {
                Code = "EXPIRED",
                ExpiresAt = DateTime.UtcNow.AddDays(-1) // Set in the past
            };
            _mockRepo.Setup(r => r.GetByCodeAsync("EXPIRED")).ReturnsAsync(expiredFile);

            var result = await _controller.GetFileInfo("EXPIRED");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(410, objectResult.StatusCode);
            Assert.Contains("Link expired", objectResult.Value.ToString());
        }

        [Fact]
        public async Task GetFileInfo_DownloadLimitReached_ReturnsGone()
        {
            var limitReachedFile = new FileMetadata
            {
                Code = "MAXOUT",
                DownloadCount = 10,
                MaxDownloads = 10
            };
            _mockRepo.Setup(r => r.GetByCodeAsync("MAXOUT")).ReturnsAsync(limitReachedFile);

            var result = await _controller.GetFileInfo("MAXOUT");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(410, objectResult.StatusCode);
            Assert.Contains("Download limit reached", objectResult.Value.ToString());
        }

        // --- MISSING DOWNLOAD TESTS ---
        [Fact]
        public async Task DownloadFile_FileNotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetByCodeAsync("MISSING")).ReturnsAsync((FileMetadata)null);

            var result = await _controller.DownloadFile("MISSING");

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("File not found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task DownloadFile_FileExpired_ReturnsGone()
        {
            var expiredFile = new FileMetadata
            {
                Code = "EXPIRED",
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            };
            _mockRepo.Setup(r => r.GetByCodeAsync("EXPIRED")).ReturnsAsync(expiredFile);

            var result = await _controller.DownloadFile("EXPIRED");

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(410, objectResult.StatusCode);
            Assert.Contains("This link has expired", objectResult.Value.ToString());
        }
        [Fact]
        public async Task DownloadFile_PhysicalFileMissing_ReturnsNotFound()
        {
            // Path.GetTempPath() generates a 100% valid absolute path dynamically 
            // based on the OS (e.g., C:\Temp\ on Windows, or /tmp/ on Linux).
            string crossPlatformMissingPath = Path.Combine(Path.GetTempPath(), "fake_path_that_does_not_exist.jpg");

            var validMetadataMissingFile = new FileMetadata
            {
                Code = "NOFILE",
                StoragePath = crossPlatformMissingPath
            };

            _mockRepo.Setup(r => r.GetByCodeAsync("NOFILE")).ReturnsAsync(validMetadataMissingFile);

            var result = await _controller.DownloadFile("NOFILE");

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("physical file is missing", notFoundResult.Value.ToString());
        }
        [Fact]
        public async Task UploadFile_NullFile_ReturnsBadRequest()
        {
            // Act by passing null directly instead of a mocked file
            var result = await _controller.UploadFile(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("No file uploaded", badRequestResult.Value.ToString());
        }
        [Fact]
        public async Task DownloadFile_ValidFile_ReturnsFileStream()
        {
            // 1. ARRANGE: Create a real temporary file on the disk so System.IO.File.Exists passes
            var tempFilePath = Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, "fake file content");

            try
            {
                var validFile = new FileMetadata
                {
                    Code = "SUCCESS",
                    StoragePath = tempFilePath, // Point to our real temp file
                    MimeType = "text/plain",
                    OriginalFileName = "test.txt",
                    DownloadCount = 0,
                    MaxDownloads = 10
                };

                _mockRepo.Setup(r => r.GetByCodeAsync("SUCCESS")).ReturnsAsync(validFile);

                // 2. ACT
                var result = await _controller.DownloadFile("SUCCESS");

                // 3. ASSERT
                // Verify it returns the actual file stream
                var fileResult = Assert.IsType<FileStreamResult>(result);
                Assert.Equal("text/plain", fileResult.ContentType);
                Assert.Equal("test.txt", fileResult.FileDownloadName);

                // Verify the DownloadCount was updated in the database
                _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<FileMetadata>()), Times.Once);
            }
            finally
            {
                // 4. CLEANUP: Delete the physical file so we don't clutter your hard drive
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }
        [Fact]
        public async Task DownloadFile_ValidFileWithFutureExpiry_ReturnsFileStream()
        {
            // 1. ARRANGE
            var tempFilePath = Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, "future file content");

            try
            {
                var validFile = new FileMetadata
                {
                    Code = "FUTURE",
                    StoragePath = tempFilePath,
                    MimeType = "text/plain",
                    OriginalFileName = "future.txt",
                    DownloadCount = 0,
                    MaxDownloads = 10,
                    // THIS is the missing branch condition:
                    ExpiresAt = DateTime.UtcNow.AddDays(5)
                };

                _mockRepo.Setup(r => r.GetByCodeAsync("FUTURE")).ReturnsAsync(validFile);

                // 2. ACT
                var result = await _controller.DownloadFile("FUTURE");

                // 3. ASSERT
                var fileResult = Assert.IsType<FileStreamResult>(result);
                Assert.Equal("text/plain", fileResult.ContentType);
            }
            finally
            {
                // 4. CLEANUP
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }
    }
}