using System.ComponentModel.DataAnnotations;
using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Models.DTOs
{
    /// <summary>
    /// DTO nhận data từ Admin Form để tạo/chỉnh sửa Coupon.
    /// Validation được xử lý bằng Data Annotations tại tầng này
    /// trước khi truyền xuống Service.
    /// </summary>
    public class CouponFormDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mã giảm giá.")]
        [MaxLength(50, ErrorMessage = "Mã tối đa 50 ký tự.")]
        [RegularExpression(@"^[A-Z0-9_\-]{2,50}$",
            ErrorMessage = "Mã chỉ gồm chữ IN HOA, số, dấu _ và -. Ví dụ: SUMMER2025")]
        public string Code { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "Mô tả tối đa 200 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại giảm giá.")]
        public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

        // Không bắt buộc với FreeShipping
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm phải >= 0.")]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giới hạn giảm phải >= 0.")]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn tối thiểu phải >= 0.")]
        public decimal MinOrderValue { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày hết hạn.")]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddDays(30);

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn lượt phải >= 1.")]
        public int? UsageLimit { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
