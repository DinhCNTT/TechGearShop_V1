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
        private readonly IExcelExportService _excelExportService;

        public DashboardController(IDashboardService dashboardService, IExcelExportService excelExportService)
        {
            _dashboardService = dashboardService;
            _excelExportService = excelExportService;
        }

        /// <summary>
        /// Hiển thị Dashboard. Nếu không truyền month/year thì dùng tháng hiện tại (múi giờ VN).
        /// </summary>
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var model = await _dashboardService.GetDashboardDataAsync(month, year);
            return View(model);
        }

        public async Task<IActionResult> ExportExcel(int? month, int? year)
        {
            var model = await _dashboardService.GetDashboardDataAsync(month, year);
            var excelData = _excelExportService.GenerateDashboardExcelReport(model, model.SelectedMonth, model.SelectedYear);

            string fileName = $"BaoCao_TechGear_T{model.SelectedMonth}_{model.SelectedYear}.xlsx";
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
