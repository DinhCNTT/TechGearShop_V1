using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _dashboardService.GetDashboardDataAsync();
            return View(model);
        }
    }
}
