using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface ICouponService
    {
        // ── Validation & Apply ────────────────────────────────────────────────────
        /// <summary>
        /// Validate toàn bộ điều kiện của coupon (active, date, limit, per-user, minOrder).
        /// Trả về CouponValidationResult với discount đã tính và error message nếu fail.
        /// </summary>
        Task<CouponValidationResult> ValidateCouponAsync(string code, decimal subTotal, decimal shippingFee, int? userId);

        /// <summary>
        /// Ghi lại lịch sử sử dụng và increment UsageCount sau khi đơn hàng xác nhận.
        /// Được gọi bởi OrderProcessingBackgroundService, không phải khi user chỉ xem preview.
        /// </summary>
        Task RecordUsageAsync(string code, int userId, int orderId);

        // ── Admin CRUD ────────────────────────────────────────────────────────────
        Task<IEnumerable<Coupon>> GetAllCouponsAsync();
        Task<Coupon?> GetByIdAsync(int id);
        Task<Coupon> CreateCouponAsync(CouponFormDto dto);
        Task UpdateCouponAsync(int id, CouponFormDto dto);
        Task DeleteCouponAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }

    /// <summary>Kết quả validate coupon: discount đã tính, shipping discount, và message lỗi.</summary>
    public class CouponValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal ShippingDiscount { get; set; }
        public int? CouponId { get; set; }
    }
}
