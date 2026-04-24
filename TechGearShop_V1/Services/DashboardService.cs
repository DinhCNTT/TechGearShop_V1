using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    /// <summary>
    /// Cung cấp dữ liệu thống kê real-time cho Admin Dashboard.
    /// Tối ưu: mỗi query chỉ truy xuất đúng trường cần thiết, không load toàn bộ entity.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // ── Chạy tuần tự: EF Core DbContext không thread-safe, không dùng Task.WhenAll ──
            var todayOrders     = await _context.Orders.CountAsync(o => o.OrderDate >= today);
            var monthlyRevenue  = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfMonth && o.Status == OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.FinalAmount) ?? 0;
            var lowStock        = await _context.Products.CountAsync(p => p.IsActive && p.Stock <= 5);
            var newUsers        = await _context.Users.CountAsync(u => u.CreatedAt >= firstDayOfMonth);
            var pendingOrders   = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);

            // ── Doanh thu + số đơn 6 tháng gần nhất ──
            var sixMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
            var revenueByMonth = await _context.Orders
                .Where(o => o.OrderDate >= sixMonthsAgo && o.Status == OrderStatus.Completed)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.FinalAmount),
                    Count   = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // Đảm bảo đủ 6 tháng kể cả tháng không có đơn (fill zero)
            var labels = new List<string>();
            var revenues = new List<decimal>();
            var orderCounts = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var target = now.AddMonths(-i);
                var entry = revenueByMonth.FirstOrDefault(x => x.Year == target.Year && x.Month == target.Month);
                labels.Add($"T{target.Month}/{target.Year % 100}");
                revenues.Add(entry?.Revenue ?? 0);
                orderCounts.Add(entry?.Count ?? 0);
            }

            // ── Top 5 sản phẩm bán chạy nhất ──
            var topProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => new { od.ProductId, od.Product!.Name })
                .Select(g => new {
                    Name    = g.Key.Name,
                    SoldQty = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.SoldQty)
                .Take(5)
                .ToListAsync();

            // ── 5 đơn hàng mới nhất ──
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new {
                    o.Id, o.FinalAmount, o.Status, o.OrderDate,
                    CustomerName = o.User != null ? o.User.Username : "Khách"
                })
                .ToListAsync();

            return new DashboardViewModel
            {
                TodayNewOrders       = todayOrders,
                MonthlyRevenue       = monthlyRevenue,
                LowStockProductCount = lowStock,
                NewUsersThisMonth    = newUsers,
                PendingOrders        = pendingOrders,

                RevenueLabels  = labels,
                RevenueData    = revenues,
                OrderCountData = orderCounts,

                TopProducts  = topProducts.Select(x => new TopProductItem(x.Name, x.SoldQty, x.Revenue)).ToList(),
                RecentOrders = recentOrders.Select(x => new RecentOrderItem(x.Id, x.CustomerName, x.FinalAmount, x.Status.ToString(), x.OrderDate)).ToList()
            };
        }
    }
}
