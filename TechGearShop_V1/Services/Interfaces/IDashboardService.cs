using TechGearShop_V1.Models.ViewModels;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IDashboardService
    {
        /// <summary>
        /// Lấy dữ liệu dashboard cho tháng/năm được chọn.
        /// Nếu month/year là null, mặc định dùng tháng hiện tại (múi giờ VN).
        /// </summary>
        Task<DashboardViewModel> GetDashboardDataAsync(int? month = null, int? year = null);
    }
}
