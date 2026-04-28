namespace TechGearShop_V1.Models.ViewModels
{
    /// <summary>ViewModel cho Dashboard Admin — các chỉ số KPI + Chart data</summary>
    public class DashboardViewModel
    {
        // ── Bộ lọc tháng/năm (do Controller truyền vào, dùng để giữ state trên UI) ──
        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }

        // ── KPI Cards ──
        public int TodayNewOrders { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int LowStockProductCount { get; set; }   // Tồn kho <= 5
        public int NewUsersThisMonth { get; set; }
        public int PendingOrders { get; set; }

        // ── Chỉ số Tăng trưởng (%) so với kỳ trước ──
        public double RevenueGrowthPercent { get; set; }    // VD: +12.5 hoặc -3.2
        public double OrderGrowthPercent { get; set; }
        public double UserGrowthPercent { get; set; }

        // ── Chart: Doanh thu + số đơn 6 tháng gần kỳ chọn ──
        public List<string> RevenueLabels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();
        public List<int> OrderCountData { get; set; } = new();

        // ── Top sản phẩm bán chạy ──
        public List<TopProductItem> TopProducts { get; set; } = new();

        // ── Đơn hàng mới nhất (5 cái) ──
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    public record TopProductItem(string Name, int SoldQty, decimal Revenue);
    public record RecentOrderItem(int Id, string CustomerName, decimal FinalAmount, string Status, DateTime OrderDate);
}
