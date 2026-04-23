using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Models.ViewModels
{
    public class ProductCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [Display(Name = "Tên sản phẩm")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Hãng sản xuất")]
        [MaxLength(100)]
        public string? Brand { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Display(Name = "Giá bán (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Display(Name = "Giá khuyến mãi (VNĐ)")]
        public decimal? PromotionalPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Display(Name = "Số lượng kho")]
        [Range(0, 100000, ErrorMessage = "Số lượng không hợp lệ")]
        public int Stock { get; set; }

        [Display(Name = "Hình ảnh sản phẩm")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImagePath { get; set; }

        [Display(Name = "Mô tả sản phẩm")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    }
}
