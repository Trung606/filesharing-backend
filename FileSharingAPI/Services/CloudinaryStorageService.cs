using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FileSharingAPI.Services;

namespace FileSharingAPI.Services
{
    public class CloudinaryStorageService : IStorageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryStorageService(IConfiguration config)
        {
            // Pulls your API keys from appsettings.json
            Account account = new Account(
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
                var uploadParams = new RawUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    // Names the file in Cloudinary: e.g., "7BA151_Screenshot.png"
                    PublicId = $"{uniqueCode}_{file.FileName}"
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task DeleteFileAsync(string filePath)
        {
            // 1. Cloudinary URLs look like this: https://res.cloudinary.com/.../raw/upload/v1234/CODE_file.png
            // We need to extract just the "CODE_file.png" part to tell Cloudinary what to delete.
            var publicId = filePath.Split('/').Last();
            publicId = Uri.UnescapeDataString(publicId); // Fixes spaces (e.g., %20)

            // 2. Set up the deletion command. Since you upload files as "raw" assets (as seen in your URLs), 
            // we must tell Cloudinary to look for a "Raw" resource type.
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Raw
            };

            // 3. Send the secure destroy command to the cloud
            await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}