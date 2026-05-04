using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class VNPayController : Controller
    {
        private readonly IVNPayService _vnPayService;
        private readonly IOrderService _orderService;

        public VNPayController(IVNPayService vnPayService, IOrderService orderService)
        {
            _vnPayService = vnPayService;
            _orderService = orderService;
        }

        public async Task<IActionResult> Pay(int orderId)
        {
            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (order == null || order.Status != Models.Entities.OrderStatus.PaymentPending)
            {
                return RedirectToAction("Index", "Home");
            }

            var url = _vnPayService.CreatePaymentUrl(order.Id, order.FinalAmount, $"Thanh toan don hang {order.Id} tai TechGearShop", HttpContext);
            return Redirect(url);
        }

        // Return URL: Trình duyệt redirect về đây sau khi thanh toán
        public async Task<IActionResult> Return()
        {
            var response = _vnPayService.ValidatePaymentResponse(Request.Query);

            // Cập nhật DB ngay tại đây (fallback khi IPN không gọi được qua localhost)
            if (int.TryParse(response.OrderId, out int orderId))
            {
                if (response.IsSuccess)
                    await _orderService.ConfirmPaymentAsync(orderId, response.TransactionId, response.ResponseCode);
                else
                    await _orderService.FailPaymentAsync(orderId, response.ResponseCode);
            }

            return View(response);
        }

        // IPN URL: Webhook gọi từ server VNPay (Server-to-Server)
        [HttpGet]
        public async Task<IActionResult> IPN()
        {
            try
            {
                var response = _vnPayService.ValidatePaymentResponse(Request.Query);

                if (int.TryParse(response.OrderId, out int orderId))
                {
                    if (response.IsSuccess)
                    {
                        // Thanh toán thành công
                        bool success = await _orderService.ConfirmPaymentAsync(orderId, response.TransactionId, response.ResponseCode);
                        if (success)
                        {
                            return Ok(new { RspCode = "00", Message = "Confirm Success" });
                        }
                    }
                    else
                    {
                        // Thanh toán thất bại hoặc user hủy
                        bool success = await _orderService.FailPaymentAsync(orderId, response.ResponseCode);
                        if (success)
                        {
                            return Ok(new { RspCode = "00", Message = "Confirm Success" });
                        }
                    }
                }
                
                return Ok(new { RspCode = "01", Message = "Order not found" });
            }
            catch (Exception ex)
            {
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }
    }
}
