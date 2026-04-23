using Microsoft.AspNetCore.Http;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IImageService
    {
        Task<string> UploadImageWithWatermarkAsync(IFormFile file, string subFolder = "products");
    }
}
