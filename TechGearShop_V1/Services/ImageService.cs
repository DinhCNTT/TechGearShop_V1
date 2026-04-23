using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;

        public ImageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadImageWithWatermarkAsync(IFormFile file, string subFolder = "products")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            // 1. Setup đường dẫn lưu file
            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", subFolder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 2. Dùng ImageSharp đóng watermark
            using (var image = await Image.LoadAsync(file.OpenReadStream()))
            {
                // Thay đổi kích thước (Resize ảnh) cho chuẩn hóa
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(800, 800),
                    Mode = ResizeMode.Max
                }));

                // Đóng Watermark bằng Text (hoặc lấy file logo)
                var font = SystemFonts.CreateFont("Arial", 40);
                var textOptions = new RichTextOptions(font)
                {
                    Origin = new PointF(image.Width - 400, image.Height - 100),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                // Vẽ text "TechGear Shop Mới" mờ 50%
                image.Mutate(x => x.DrawText(
                    textOptions, 
                    "TechGear Shop", 
                    Color.White.WithAlpha(0.5f))
                );

                // Lưu ảnh ra
                await image.SaveAsync(filePath);
            }

            return $"/images/{subFolder}/{uniqueFileName}";
        }
    }
}
