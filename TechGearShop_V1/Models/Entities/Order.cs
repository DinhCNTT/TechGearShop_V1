using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechGearShop_V1.Models.Entities
{
    public class Order
    {
        public int Id { get; set; }

        // FK -> User
        public int UserId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        // Tổng cuối = TotalAmount + ShippingFee - DiscountAmount
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        // Mã coupon đã dùng (để ghi nhận, có thể null)
        [MaxLength(50)]
        public string? CouponCode { get; set; }

        // Thông tin giao hàng
        [Required, MaxLength(200)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Province { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
