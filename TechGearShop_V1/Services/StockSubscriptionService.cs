using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.Enums;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class StockSubscriptionService : IStockSubscriptionService
    {
        private readonly AppDbContext _context;

        public StockSubscriptionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> SubscribeAsync(int productId, int? userId, string guestEmail)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return (false, "Sản phẩm không tồn tại.");
            if (product.Stock > 0) return (false, "Sản phẩm hiện đang có hàng, không cần nhận thông báo.");

            bool alreadySubscribed = false;
            
            if (userId.HasValue)
            {
                alreadySubscribed = await _context.StockSubscriptions
                    .AnyAsync(s => s.ProductId == productId && s.UserId == userId.Value && s.Status == StockSubscriptionStatus.Pending);
            }
            else if (!string.IsNullOrEmpty(guestEmail))
            {
                alreadySubscribed = await _context.StockSubscriptions
                    .AnyAsync(s => s.ProductId == productId && s.GuestEmail == guestEmail && s.Status == StockSubscriptionStatus.Pending);
            }
            else
            {
                return (false, "Cần cung cấp Email tĩnh hoặc Yêu cầu Đăng nhập.");
            }

            if (alreadySubscribed)
            {
                return (false, "Bạn đã đăng ký nhận thông báo cho sản phẩm này rồi.");
            }

            var subscription = new StockSubscription
            {
                ProductId = productId,
                UserId = userId,
                GuestEmail = guestEmail,
                Status = StockSubscriptionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return (true, "Đăng ký nhận thông báo thành công!");
        }
    }
}
