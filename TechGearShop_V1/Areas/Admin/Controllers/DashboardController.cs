using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Hiển thị Dashboard. Nếu không truyền month/year thì dùng tháng hiện tại (múi giờ VN).
        /// </summary>
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var model = await _dashboardService.GetDashboardDataAsync(month, year);
            return View(model);
        }
    }
}
