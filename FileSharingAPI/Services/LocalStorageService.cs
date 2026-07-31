namespace FileSharingAPI.Services
{
    public class LocalStorageService : IStorageService
    {
        public async Task<string> SaveFileAsync(IFormFile file, string uniqueCode)
        {
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadDirectory)) Directory.CreateDirectory(uploadDirectory);

            var filePath = Path.Combine(uploadDirectory, $"{uniqueCode}_{file.FileName}");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return filePath; // Return the path to save in the database
        }

        public Task DeleteFileAsync(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            return Task.CompletedTask;
        }
    }
}