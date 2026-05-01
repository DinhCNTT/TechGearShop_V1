using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechGearShop_V1.Models.Entities
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public int UserId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required, MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Cờ xác nhận người dùng thực sự đã mua hàng (luôn là true theo logic mới)
        public bool IsVerifiedPurchase { get; set; } = true;

        // Navigation properties
        public Product? Product { get; set; }
        public User? User { get; set; }
    }
}
