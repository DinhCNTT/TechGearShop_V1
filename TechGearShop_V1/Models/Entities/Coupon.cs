using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechGearShop_V1.Models.Entities
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        // Giá trị chiết khấu (VD: 50000 = giảm 50k, hoặc 10 = giảm 10%)
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        // true = theo %, false = theo số tiền cố định
        public bool IsPercentage { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderValue { get; set; } = 0;

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Giới hạn số lần dùng (null = không giới hạn)
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; } = 0;
    }
}
