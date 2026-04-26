using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechGearShop_V1.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Brand { get; set; }

        // FK -> Category
        public int CategoryId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PromotionalPrice { get; set; }

        public int Stock { get; set; } = 0;

        [MaxLength(500)]
        public string? ImagePath { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Denormalized fields for performance
        public bool IsFeatured { get; set; } = false;
        public int SoldCount { get; set; } = 0;
        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Category? Category { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    }
}
