using Microsoft.AspNetCore.Http;

namespace TechGearShop_V1.Services.Interfaces
{
    public class MediaUploadResult
    {
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
    }

    public interface IImageService
    {
        Task<string> UploadImageWithWatermarkAsync(IFormFile file, string subFolder = "products");
        Task<List<MediaUploadResult>> UploadMultipleImagesAsync(List<IFormFile> files, string subFolder = "products");
        Task DeleteImageByPublicIdAsync(string publicId);
    }
}
