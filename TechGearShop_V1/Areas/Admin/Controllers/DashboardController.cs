using Microsoft.AspNetCore.Mvc;

namespace TechGearShop_V1.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Admin")] // TODO: Bỏ comment khi làm Auth
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
