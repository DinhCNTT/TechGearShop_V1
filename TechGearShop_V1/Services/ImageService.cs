using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class ImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;

        public ImageService(IConfiguration config)
        {
            var acc = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadImageWithWatermarkAsync(IFormFile file, string subFolder = "products")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "TechGearShop/" + subFolder,
                
                // Cloudinary tích hợp resize & watermark text cực xịn xò
                Transformation = new Transformation()
                    .Width(800).Height(800).Crop("limit") // Đảm bảo hình ảnh ko bị quá khủng
                    .Chain()
                    .Overlay(new Layer().PublicId("text:Arial_40_bold:TechGear%20Shop"))
                    .Color("white").Opacity(50).Gravity("south_east").X(20).Y(20)
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            
            if (uploadResult.Error != null)
            {
                throw new Exception("Lỗi Cloudinary: " + uploadResult.Error.Message);
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<List<MediaUploadResult>> UploadMultipleImagesAsync(List<IFormFile> files, string subFolder = "products")
        {
            var results = new List<MediaUploadResult>();
            if (files == null || files.Count == 0) return results;

            var uploadTasks = files.Select(async file =>
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "TechGearShop/" + subFolder,
                    Transformation = new Transformation()
                        .Width(800).Height(800).Crop("limit")
                        .Chain()
                        .Overlay(new Layer().PublicId("text:Arial_40_bold:TechGear%20Shop"))
                        .Color("white").Opacity(50).Gravity("south_east").X(20).Y(20)
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                {
                    throw new Exception("Lỗi Cloudinary: " + uploadResult.Error.Message);
                }
                return new MediaUploadResult 
                { 
                    Url = uploadResult.SecureUrl.ToString(), 
                    PublicId = uploadResult.PublicId 
                };
            });

            var uploadedResults = await Task.WhenAll(uploadTasks);
            results.AddRange(uploadedResults);
            
            return results;
        }

        public async Task DeleteImageByPublicIdAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return;
            var deleteParams = new DeletionParams(publicId) { ResourceType = ResourceType.Image };
            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}
