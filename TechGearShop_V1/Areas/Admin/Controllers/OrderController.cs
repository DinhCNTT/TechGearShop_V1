using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index(string searchKeyword, OrderStatus? status, int page = 1)
        {
            const int pageSize = 15; // 15 orders per page
            var (orders, totalCount) = await _orderService.GetPagedOrdersAsync(searchKeyword, status, page, pageSize);

            var model = new TechGearShop_V1.Models.ViewModels.OrderListViewModel
            {
                Orders = orders,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                SearchKeyword = searchKeyword,
                Status = status
            };

            return View(model);
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
