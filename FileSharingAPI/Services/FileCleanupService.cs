using FileSharingAPI.Repositories;
using FileSharingAPI.Services;

namespace FileSharingAPI.Services
{
    public class FileCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileCleanupService> _logger;

        public FileCleanupService(IServiceScopeFactory scopeFactory, ILogger<FileCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Running automated file cleanup task...");

                using var scope = _scopeFactory.CreateScope();

                // Grab BOTH the repository and the storage service from the scoped provider
                var repo = scope.ServiceProvider.GetRequiredService<IFileRepository>();
                var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

                var expiredFiles = await repo.GetExpiredFilesAsync();

                foreach (var file in expiredFiles)
                {
                    try
                    {
                        // 1. Delete the asset from Cloudinary using your storage service
                        await storage.DeleteFileAsync(file.StoragePath);

                        // 2. Delete the metadata row from the PostgreSQL database
                        await repo.DeleteAsync(file.Code);

                        _logger.LogInformation($"Successfully deleted expired file and Cloudinary asset: {file.Code}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to delete file: {file.Code}");
                    }
                }
            }
        }
    }
}