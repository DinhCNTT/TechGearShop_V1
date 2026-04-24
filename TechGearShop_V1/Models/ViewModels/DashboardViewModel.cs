namespace TechGearShop_V1.Models.ViewModels
{
    /// <summary>ViewModel cho Dashboard Admin — các chỉ số KPI + Chart data</summary>
    public class DashboardViewModel
    {
        // KPI Cards
        public int TodayNewOrders { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int LowStockProductCount { get; set; }   // Tồn kho <= 5
        public int NewUsersThisMonth { get; set; }
        public int PendingOrders { get; set; }

        // Chart: Doanh thu 6 tháng gần nhất
        public List<string> RevenueLabels { get; set; } = new();  // ["T1","T2",...]
        public List<decimal> RevenueData { get; set; } = new();   // [12000000,...]

        // Chart: Số đơn hàng 6 tháng gần nhất
        public List<int> OrderCountData { get; set; } = new();

        // Top sản phẩm bán chạy
        public List<TopProductItem> TopProducts { get; set; } = new();

        // Đơn hàng mới nhất (5 cái)
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    public record TopProductItem(string Name, int SoldQty, decimal Revenue);
    public record RecentOrderItem(int Id, string CustomerName, decimal FinalAmount, string Status, DateTime OrderDate);
}
