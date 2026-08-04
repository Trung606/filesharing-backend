using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace FileSharingAPI.Services
{
    public class CloudinaryStorageService : IStorageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryStorageService(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string uniqueCode)
        {
            var uploadResult = new RawUploadResult();

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    PublicId = $"{uniqueCode}_{file.FileName}"
                    // The SDK handles ResourceType automatically for RawUploadParams now
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            // Return the secure cloud URL instead of a local file path
            return uploadResult.SecureUrl.ToString();
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            try
            {
                // Cloudinary requires the PublicId to delete. 
                // We extract the filename from the end of the URL.
                var uri = new Uri(fileUrl);
                var publicId = Path.GetFileNameWithoutExtension(uri.LocalPath);

                var deleteParams = new DelResParams
                {
                    PublicIds = new List<string> { publicId },
                    ResourceType = ResourceType.Raw
                };

                await _cloudinary.DeleteResourcesAsync(deleteParams);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cloudinary delete failed: {ex.Message}");
            }
        }
    }
}