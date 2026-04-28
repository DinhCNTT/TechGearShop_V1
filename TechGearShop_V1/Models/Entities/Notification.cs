using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TechGearShop_V1.Models.Enums;

namespace TechGearShop_V1.Models.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.System;

        [MaxLength(500)]
        public string? LinkTo { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
