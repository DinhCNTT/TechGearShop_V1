using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Admin")] // Uncomment khi kích hoạt Auth Admin thật
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetAllOrdersWithUsersAsync();
            return View(orders);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _orderService.GetOrderWithDetailsAsync(id);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status)
        {
            await _orderService.UpdateOrderStatusAsync(orderId, status);
            TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng #{orderId} thành {status} thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
