using Microsoft.EntityFrameworkCore;
using FileSharingAPI.Data;
using FileSharingAPI.Models;

namespace FileSharingAPI.Repositories
{
    public class FileRepository : IFileRepository
    {
        private readonly ApplicationDbContext _context;

        public FileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FileMetadata> AddAsync(FileMetadata file)
        {
            _context.Files.Add(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<FileMetadata?> GetByCodeAsync(string code)
        {
            return await _context.Files.FirstOrDefaultAsync(f => f.Code == code);
        }
        public async Task<FileMetadata?> UpdateAsync(FileMetadata file)
        {
            _context.Files.Update(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<IEnumerable<FileMetadata>> GetExpiredFilesAsync()
        {
            // Find files where the expiration date has passed OR the download limit is reached
            return await _context.Files
                .Where(f => (f.ExpiresAt.HasValue && DateTime.UtcNow > f.ExpiresAt.Value)
                         || f.DownloadCount >= f.MaxDownloads)
                .ToListAsync();
        }

        public async Task DeleteAsync(string code)
        {
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Code == code);
            if (file != null)
            {
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();
            }
        }
    }
}