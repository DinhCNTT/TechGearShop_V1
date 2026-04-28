using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    /// <summary>
    /// Cung cấp dữ liệu thống kê cho Admin Dashboard.
    /// - Múi giờ: Luôn dùng "SE Asia Standard Time" (UTC+7) để tránh sai lệch ngày.
    /// - Bộ lọc: Nhận tháng/năm tuỳ chọn; mặc định là tháng hiện tại.
    /// - Tăng trưởng: Tính % so với cùng kỳ trước (MoM - Month over Month).
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        // Múi giờ Việt Nam — dùng TimeZoneInfo.FindSystemTimeZoneById để an toàn đa nền tảng
        private static readonly TimeZoneInfo _vnTz = GetVnTimeZone();

        private static TimeZoneInfo GetVnTimeZone()
        {
            // Windows: "SE Asia Standard Time" | Linux/macOS: "Asia/Ho_Chi_Minh"
            try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        }

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(int? month = null, int? year = null)
        {
            // ── 1. Xác định kỳ hiện tại (tháng/năm được chọn) theo múi giờ VN ──
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTz);

            int selectedYear  = year  ?? nowVn.Year;
            int selectedMonth = month ?? nowVn.Month;

            // Kẹp giá trị hợp lệ (tránh month=0 hay month=13)
            selectedMonth = Math.Clamp(selectedMonth, 1, 12);
            // Không cho phép chọn tương lai
            var selectedDate = new DateTime(selectedYear, selectedMonth, 1);
            if (selectedDate > new DateTime(nowVn.Year, nowVn.Month, 1))
            {
                selectedYear  = nowVn.Year;
                selectedMonth = nowVn.Month;
            }

            var firstDayOfPeriod = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var firstDayOfNext   = firstDayOfPeriod.AddMonths(1);

            // ── 2. Xác định kỳ TRƯỚC để so sánh ──
            var firstDayOfPrev     = firstDayOfPeriod.AddMonths(-1);
            var firstDayOfPrevNext = firstDayOfPeriod;

            // Chuyển range thành UTC để query DB (DB lưu UTC)
            var curStart  = TimeZoneInfo.ConvertTimeToUtc(firstDayOfPeriod, _vnTz);
            var curEnd    = TimeZoneInfo.ConvertTimeToUtc(firstDayOfNext, _vnTz);
            var prevStart = TimeZoneInfo.ConvertTimeToUtc(firstDayOfPrev, _vnTz);
            var prevEnd   = curStart; // kỳ trước kết thúc lúc kỳ hiện tại bắt đầu

            // ── 3. Truy vấn KPI Cards ──
            // "Hôm nay" luôn tính theo ngày VN thực tế, không theo kỳ chọn
            var todayVnStart = TimeZoneInfo.ConvertTimeToUtc(nowVn.Date, _vnTz);
            var todayVnEnd   = todayVnStart.AddDays(1);
            var todayOrders  = await _context.Orders
                .CountAsync(o => o.OrderDate >= todayVnStart && o.OrderDate < todayVnEnd);

            // Doanh thu kỳ hiện tại
            var currentRevenue = await _context.Orders
                .Where(o => o.OrderDate >= curStart && o.OrderDate < curEnd && o.Status == OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.FinalAmount) ?? 0;

            // Doanh thu kỳ trước
            var previousRevenue = await _context.Orders
                .Where(o => o.OrderDate >= prevStart && o.OrderDate < prevEnd && o.Status == OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.FinalAmount) ?? 0;

            // Đơn hàng kỳ hiện tại (hoàn thành)
            var currentOrderCount = await _context.Orders
                .CountAsync(o => o.OrderDate >= curStart && o.OrderDate < curEnd && o.Status == OrderStatus.Completed);

            // Đơn hàng kỳ trước (hoàn thành)
            var previousOrderCount = await _context.Orders
                .CountAsync(o => o.OrderDate >= prevStart && o.OrderDate < prevEnd && o.Status == OrderStatus.Completed);

            // Người dùng mới kỳ hiện tại
            var currentNewUsers = await _context.Users
                .CountAsync(u => u.CreatedAt >= curStart && u.CreatedAt < curEnd);

            // Người dùng mới kỳ trước
            var previousNewUsers = await _context.Users
                .CountAsync(u => u.CreatedAt >= prevStart && u.CreatedAt < prevEnd);

            var lowStock      = await _context.Products.CountAsync(p => p.IsActive && p.Stock <= 5);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);

            // ── 4. Biểu đồ 6 kỳ gần nhất tính từ kỳ được chọn ──
            var sixPeriodsAgo = firstDayOfPeriod.AddMonths(-5);
            var chartStartUtc = TimeZoneInfo.ConvertTimeToUtc(sixPeriodsAgo, _vnTz);

            var revenueByMonth = await _context.Orders
                .Where(o => o.OrderDate >= chartStartUtc && o.OrderDate < curEnd && o.Status == OrderStatus.Completed)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.FinalAmount),
                    Count   = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var labels      = new List<string>();
            var revenues    = new List<decimal>();
            var orderCounts = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var target = firstDayOfPeriod.AddMonths(-i);
                // Chuyển về UTC để giao, nhưng group theo VN time: dùng index tháng/năm VN
                var entry = revenueByMonth.FirstOrDefault(x => x.Year == target.Year && x.Month == target.Month);
                labels.Add($"T{target.Month}/{target.Year % 100}");
                revenues.Add(entry?.Revenue ?? 0);
                orderCounts.Add(entry?.Count ?? 0);
            }

            // ── 5. Top 5 sản phẩm bán chạy (mọi thời gian) ──
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

            // ── 6. 5 đơn hàng mới nhất ──
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
                SelectedMonth = selectedMonth,
                SelectedYear  = selectedYear,

                TodayNewOrders       = todayOrders,
                MonthlyRevenue       = currentRevenue,
                LowStockProductCount = lowStock,
                NewUsersThisMonth    = currentNewUsers,
                PendingOrders        = pendingOrders,

                RevenueGrowthPercent = CalcGrowth(currentRevenue, previousRevenue),
                OrderGrowthPercent   = CalcGrowth(currentOrderCount, previousOrderCount),
                UserGrowthPercent    = CalcGrowth(currentNewUsers, previousNewUsers),

                RevenueLabels  = labels,
                RevenueData    = revenues,
                OrderCountData = orderCounts,

                TopProducts  = topProducts.Select(x => new TopProductItem(x.Name, x.SoldQty, x.Revenue)).ToList(),
                RecentOrders = recentOrders.Select(x => new RecentOrderItem(x.Id, x.CustomerName, x.FinalAmount, x.Status.ToString(), x.OrderDate)).ToList()
            };
        }

        /// <summary>
        /// Tính phần trăm tăng trưởng (MoM).
        /// Trả về 0 nếu kỳ trước không có dữ liệu (tránh chia cho 0).
        /// </summary>
        private static double CalcGrowth(decimal current, decimal previous)
        {
            if (previous == 0) return current > 0 ? 100.0 : 0.0;
            return Math.Round((double)((current - previous) / previous * 100), 1);
        }

        private static double CalcGrowth(int current, int previous)
            => CalcGrowth((decimal)current, (decimal)previous);
    }
}

