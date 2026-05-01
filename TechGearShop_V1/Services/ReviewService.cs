using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanUserReviewAsync(int userId, int productId)
        {
            // Kiểm tra xem user có đơn hàng nào CHUẨN BỊ (Completed) chứa sản phẩm này không
            return await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Completed)
                .SelectMany(o => o.OrderDetails)
                .AnyAsync(od => od.ProductId == productId);
        }

        public async Task<(bool Success, string Message)> AddReviewAsync(int userId, int productId, int rating, string content)
        {
            if (rating < 1 || rating > 5)
            {
                return (false, "Điểm đánh giá phải từ 1 đến 5 sao.");
            }

            // Kiểm tra điều kiện bắt buộc: Đã mua hàng
            bool canReview = await CanUserReviewAsync(userId, productId);
            if (!canReview)
            {
                return (false, "Bạn phải mua và nhận thành công sản phẩm này mới được phép đánh giá.");
            }

            // Kiểm tra xem đã đánh giá chưa để Update (Upsert)
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (existingReview != null)
                {
                    // Update
                    existingReview.Rating = rating;
                    existingReview.Content = string.IsNullOrWhiteSpace(content) ? "" : content.Trim();
                    existingReview.CreatedAt = DateTime.UtcNow; // Cập nhật lại thời gian sửa
                    _context.Reviews.Update(existingReview);
                }
                else
                {
                    // Insert
                    var review = new Review
                    {
                        ProductId = productId,
                        UserId = userId,
                        Rating = rating,
                        Content = string.IsNullOrWhiteSpace(content) ? "" : content.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        IsVerifiedPurchase = true
                    };
                    _context.Reviews.Add(review);
                }

                await _context.SaveChangesAsync();

                // Cập nhật Product
                var stats = await _context.Reviews
                    .Where(r => r.ProductId == productId)
                    .GroupBy(r => 1)
                    .Select(g => new
                    {
                        Count = g.Count(),
                        Average = (decimal)g.Average(r => (double)r.Rating)
                    })
                    .FirstOrDefaultAsync();

                if (stats != null)
                {
                    // Update dùng ExecuteUpdateAsync cho nhanh gọn và nguyên tử
                    await _context.Products
                        .Where(p => p.Id == productId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.ReviewCount, stats.Count)
                            .SetProperty(p => p.AverageRating, Math.Round(stats.Average, 1)));
                }

                await transaction.CommitAsync();
                
                string actionMsg = existingReview != null ? "Cập nhật đánh giá thành công!" : "Gửi đánh giá thành công!";
                return (true, actionMsg);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return (false, "Đã có lỗi xảy ra khi lưu đánh giá. Vui lòng thử lại.");
            }
        }

        public async Task<PaginatedReviewDto> GetReviewsPaginatedAsync(int productId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .AsNoTracking();

            int totalReviews = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);

            // Tính phân bố sao
            var ratingDistribution = new int[5];
            var distributionData = await query
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in distributionData)
            {
                if (item.Rating >= 1 && item.Rating <= 5)
                {
                    ratingDistribution[item.Rating - 1] = item.Count;
                }
            }

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    Username = r.User != null ? r.User.Username : "Khách ẩn danh",
                    Rating = r.Rating,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt,
                    IsVerifiedPurchase = r.IsVerifiedPurchase
                })
                .ToListAsync();

            var product = await _context.Products.AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => new { p.AverageRating })
                .FirstOrDefaultAsync();

            return new PaginatedReviewDto
            {
                Reviews = reviews,
                TotalReviews = totalReviews,
                CurrentPage = page,
                TotalPages = totalPages,
                AverageRating = product?.AverageRating ?? 0,
                RatingDistribution = ratingDistribution
            };
        }
        public async Task<ReviewDto?> GetMyReviewAsync(int userId, int productId)
        {
            return await _context.Reviews
                .Where(r => r.UserId == userId && r.ProductId == productId)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    Username = r.User != null ? r.User.Username : "",
                    Rating = r.Rating,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt,
                    IsVerifiedPurchase = r.IsVerifiedPurchase
                })
                .FirstOrDefaultAsync();
        }
    }
}
