using System;

namespace TechGearShop_V1.Models.DTOs
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsAdminReply { get; set; }
        public int? ParentId { get; set; }
        /// <summary>Tên người được trả lời (để hiện quote)</summary>
        public string? ParentUsername { get; set; }
        /// <summary>Nội dung trích dẫn (tối đa 60 ký tự)</summary>
        public string? ParentContent { get; set; }
    }
}
