using System;
using System.ComponentModel.DataAnnotations;

namespace FileSharingAPI.Models
{
    public class FileMetadata
    {
        // Explicitly defining the string as the Primary Key
        [Key]
        public string Code { get; set; } = string.Empty;

        public string OriginalFileName { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public string StoragePath { get; set; } = string.Empty;

        public int MaxDownloads { get; set; }

        public int DownloadCount { get; set; } = 0;

        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}