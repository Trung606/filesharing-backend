namespace FileSharingAPI.Services
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string uniqueCode);
        Task DeleteFileAsync(string filePath);
    }
}