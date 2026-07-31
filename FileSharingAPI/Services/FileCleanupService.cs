using FileSharingAPI.Repositories;

namespace FileSharingAPI.Services
{
    // Inheriting from BackgroundService allows this to run independently in the background
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
            // Set the timer to run the cleanup task every 1 hour
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Running automated file cleanup task...");

                // We must create a scope because the Repository is Scoped, but this worker is a Singleton
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IFileRepository>();

                // Fetch dead files using the repo method you built
                var expiredFiles = await repo.GetExpiredFilesAsync();

                foreach (var file in expiredFiles)
                {
                    try
                    {
                        // 1. Delete the physical file from the local disk
                        if (File.Exists(file.StoragePath))
                        {
                            File.Delete(file.StoragePath);
                        }

                        // 2. Delete the metadata row from the PostgreSQL database
                        await repo.DeleteAsync(file.Code);

                        _logger.LogInformation($"Successfully deleted expired file: {file.Code}");
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