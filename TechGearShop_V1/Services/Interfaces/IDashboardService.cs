using TechGearShop_V1.Models.ViewModels;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}
