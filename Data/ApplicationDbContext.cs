using Microsoft.EntityFrameworkCore;
using FileSharingAPI.Models;

namespace FileSharingAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // This creates the "Files" table based on your FileMetadata model
        public DbSet<FileMetadata> Files => Set<FileMetadata>();
    }
}