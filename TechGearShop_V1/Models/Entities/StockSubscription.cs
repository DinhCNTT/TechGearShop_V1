using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TechGearShop_V1.Models.Enums;

namespace TechGearShop_V1.Models.Entities
{
    public class StockSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? GuestEmail { get; set; }

        [Required]
        public StockSubscriptionStatus Status { get; set; } = StockSubscriptionStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? NotifiedAt { get; set; }
    }
}
