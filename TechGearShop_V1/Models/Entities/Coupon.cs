using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechGearShop_V1.Models.Entities
{
    /// <summary>
    /// Loại giảm giá:
    ///   Percentage     — Giảm theo %    (vd: 20% off, tối đa MaxDiscountAmount)
    ///   FixedAmount    — Giảm số tiền cố định (vd: giảm 50.000đ)
    ///   FreeShipping   — Miễn phí vận chuyển
    /// </summary>
    public enum DiscountType
    {
        Percentage   = 0,
        FixedAmount  = 1,
        FreeShipping = 2
    }

    public class Coupon
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        // Loại giảm giá
        public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

        // Giá trị chiết khấu: % hoặc số tiền cố định (FreeShipping thì field này = 0)
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        // Giới hạn giảm tối đa khi dùng % (null = không giới hạn)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        // Đơn hàng tối thiểu để áp dụng mã
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderValue { get; set; } = 0;

        // Ngày bắt đầu hiệu lực (cho phép tạo mã trước, lên lịch ra mắt)
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Giới hạn tổng số lần dùng toàn hệ thống (null = không giới hạn)
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; } = 0;

        // Navigation
        public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();

        // ── Computed helpers ──────────────────────────────────────────────────────
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > ExpiryDate;

        [NotMapped]
        public bool IsNotStarted => DateTime.UtcNow < StartDate;

        [NotMapped]
        public bool IsExhausted => UsageLimit.HasValue && UsageCount >= UsageLimit.Value;

        [NotMapped]
        public bool IsCurrentlyValid => IsActive && !IsExpired && !IsNotStarted && !IsExhausted;
    }
}
