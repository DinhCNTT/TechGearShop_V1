namespace TechGearShop_V1.Models.Entities
{
    /// <summary>
    /// Bảng tracking lịch sử sử dụng mã giảm giá theo từng User.
    /// Unique(CouponId, UserId) được enforce cả ở DB và application level
    /// để ngăn 1 user dùng cùng 1 mã nhiều lần.
    /// </summary>
    public class CouponUsage
    {
        public int Id { get; set; }

        public int CouponId { get; set; }
        public Coupon Coupon { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Đơn hàng đã dùng mã này (nullable vì ghi sau khi đặt hàng)
        public int? OrderId { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}
