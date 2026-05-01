using System;
using System.ComponentModel.DataAnnotations;

namespace TechGearShop_V1.Models.Entities
{
    public class ProductQuestion
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(500)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// true = câu trả lời của Admin/QTV, false = câu hỏi của khách
        /// </summary>
        public bool IsAdminReply { get; set; } = false;

        /// <summary>
        /// null = câu hỏi gốc. Có giá trị = đây là câu trả lời cho câu hỏi có Id = ParentId
        /// </summary>
        public int? ParentId { get; set; }

        // Navigation properties
        public Product? Product { get; set; }
        public User? User { get; set; }
    }
}
