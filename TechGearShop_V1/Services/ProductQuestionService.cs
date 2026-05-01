using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.Enums;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class ProductQuestionService : IProductQuestionService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private const int ANTI_SPAM_SECONDS = 30;
        private const int MAX_CONTENT_LENGTH = 500;

        public ProductQuestionService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<QuestionDto>> GetHistoryAsync(int productId, int take, int skip = 0)
        {
            if (take < 1) take = 8;
            if (take > 50) take = 50;

            return await _context.ProductQuestions
                .Where(q => q.ProductId == productId)
                .OrderByDescending(q => q.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(q => q.User)
                .AsNoTracking()
                .Select(q => new QuestionDto
                {
                    Id = q.Id,
                    ProductId = q.ProductId,
                    UserId = q.UserId,
                    Username = q.User != null ? q.User.Username : "Khách",
                    Content = q.Content,
                    CreatedAt = q.CreatedAt,
                    IsAdminReply = q.IsAdminReply,
                    ParentId = q.ParentId,
                    // Join lấy thông tin câu hỏi gốc để hiện quote
                    ParentUsername = q.ParentId != null
                        ? _context.ProductQuestions
                            .Where(p => p.Id == q.ParentId)
                            .Select(p => p.User != null ? p.User.Username : "Khách")
                            .FirstOrDefault()
                        : null,
                    ParentContent = q.ParentId != null
                        ? _context.ProductQuestions
                            .Where(p => p.Id == q.ParentId)
                            .Select(p => p.Content.Length > 60 ? p.Content.Substring(0, 60) + "..." : p.Content)
                            .FirstOrDefault()
                        : null
                })
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(int productId)
        {
            return await _context.ProductQuestions
                .Where(q => q.ProductId == productId)
                .CountAsync();
        }

        public async Task<(bool Success, string Message, QuestionDto? Data)> PostQuestionAsync(
            int userId, int productId, string content, bool isAdminReply, int? parentId = null)
        {
            // === Validate ===
            if (string.IsNullOrWhiteSpace(content))
                return (false, "Nội dung không được để trống.", null);

            content = content.Trim();
            if (content.Length > MAX_CONTENT_LENGTH)
                return (false, $"Nội dung tối đa {MAX_CONTENT_LENGTH} ký tự.", null);

            // === Sanitize: Strip HTML tags để chống XSS ===
            content = Regex.Replace(content, "<.*?>", string.Empty);
            content = content.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

            // === Anti-spam: check thời gian gửi gần nhất ===
            if (!isAdminReply)
            {
                var lastQuestion = await _context.ProductQuestions
                    .Where(q => q.UserId == userId)
                    .OrderByDescending(q => q.CreatedAt)
                    .Select(q => q.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastQuestion != default)
                {
                    var elapsed = (DateTime.UtcNow - lastQuestion).TotalSeconds;
                    if (elapsed < ANTI_SPAM_SECONDS)
                    {
                        int remaining = (int)Math.Ceiling(ANTI_SPAM_SECONDS - elapsed);
                        return (false, $"Vui lòng chờ {remaining} giây trước khi gửi tiếp.", null);
                    }
                }
            }

            // === Kiểm tra sản phẩm tồn tại ===
            bool productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists)
                return (false, "Sản phẩm không tồn tại.", null);

            // === Lưu DB ===
            var question = new ProductQuestion
            {
                ProductId = productId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsAdminReply = isAdminReply,
                ParentId = parentId
            };

            _context.ProductQuestions.Add(question);
            await _context.SaveChangesAsync();

            // === Lấy thông tin câu hỏi gốc (nếu có) để trả về quote ===
            string? parentUsername = null;
            string? parentContent = null;
            if (parentId.HasValue)
            {
                var parent = await _context.ProductQuestions
                    .Where(p => p.Id == parentId.Value)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync();
                if (parent != null)
                {
                    parentUsername = parent.User?.Username ?? "Khách";
                    parentContent = parent.Content.Length > 60
                        ? parent.Content.Substring(0, 60) + "..."
                        : parent.Content;
                }
            }

            // === Lấy username của người gửi ===
            var username = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "Khách";

            var dto = new QuestionDto
            {
                Id = question.Id,
                ProductId = question.ProductId,
                UserId = question.UserId,
                Username = username,
                Content = question.Content,
                CreatedAt = question.CreatedAt,
                IsAdminReply = question.IsAdminReply,
                ParentId = question.ParentId,
                ParentUsername = parentUsername,
                ParentContent = parentContent
            };

            // === Gửi Notification ===
            var productName = await _context.Products.Where(p => p.Id == productId).Select(p => p.Name).FirstOrDefaultAsync() ?? "Sản phẩm";
            string linkTo = $"/Product/Detail/{productId}?qa={question.Id}#qa-section";

            if (isAdminReply && parentId.HasValue)
            {
                // Báo cho user của câu hỏi gốc
                var parentUserId = await _context.ProductQuestions.Where(p => p.Id == parentId.Value).Select(p => p.UserId).FirstOrDefaultAsync();
                if (parentUserId > 0)
                {
                    await _notificationService.CreateNotificationAsync(
                        parentUserId,
                        NotificationType.QnA,
                        "Admin TechGear đã trả lời bạn",
                        $"Admin đã trả lời câu hỏi của bạn về sản phẩm {productName}.",
                        linkTo
                    );
                }
            }
            else if (!isAdminReply)
            {
                // Báo cho tất cả admin
                var adminIds = await _context.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).ToListAsync();
                foreach(var adminId in adminIds)
                {
                    await _notificationService.CreateNotificationAsync(
                        adminId,
                        NotificationType.QnA,
                        "Câu hỏi mới từ khách hàng",
                        $"Khách hàng {username} vừa gửi câu hỏi cho sản phẩm {productName}.",
                        linkTo
                    );
                }
            }

            return (true, "Gửi câu hỏi thành công!", dto);
        }
    }
}
