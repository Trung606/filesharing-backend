using FileSharingAPI.Models;

namespace FileSharingAPI.Repositories
{
    public interface IFileRepository
    {
        Task<FileMetadata> AddAsync(FileMetadata file);
        Task<FileMetadata?> GetByCodeAsync(string code);

        // New methods for Module 3
        Task<FileMetadata?> UpdateAsync(FileMetadata file);
        Task<IEnumerable<FileMetadata>> GetExpiredFilesAsync();
        Task DeleteAsync(string code);
    }
}