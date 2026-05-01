using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;

namespace TechGearShop_V1.Models.DTOs
{
    /// <summary>
    /// Dữ liệu đơn hàng được đóng gói và nhét vào In-Memory Channel để xử lý bất đồng bộ.
    /// </summary>
    public class OrderRequestDto
    {
        // ── Thông tin người mua ──
        public int UserId { get; set; }

        // ── Thông tin giao hàng ──
        public string ReceiverName    { get; set; } = string.Empty;
        public string ReceiverPhone   { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string Province        { get; set; } = string.Empty;
        public string? Note           { get; set; }

        // ── Thanh toán & Coupon ──
        public PaymentMethod PaymentMethod  { get; set; } = PaymentMethod.COD;
        public string?  CouponCode          { get; set; }
        public decimal  DiscountAmount      { get; set; } = 0;
        public decimal  ShippingFee         { get; set; } = 30000;
        public decimal  SubTotal            { get; set; }
        public decimal  FinalAmount         { get; set; }

        // ── Danh sách sản phẩm ──
        public List<CartItem> Items { get; set; } = new();

        // ── Thời điểm request được tạo (để debug / log) ──
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
