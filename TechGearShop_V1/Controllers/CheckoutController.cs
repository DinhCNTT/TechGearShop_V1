using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechGearShop_V1.Extensions;
using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class CheckoutController : Controller
    {
        // Session key riêng cho luồng Reorder — không đụng vào giỏ hàng thật
        public const string REORDER_KEY = "reorder_items";

        private readonly IOrderService   _orderService;
        private readonly ICouponService  _couponService;
        private readonly IProductService _productService;
        private readonly ICartService    _cartService;

        public CheckoutController(
            IOrderService   orderService,
            ICouponService  couponService,
            IProductService productService,
            ICartService    cartService)
        {
            _orderService   = orderService;
            _couponService  = couponService;
            _productService = productService;
            _cartService    = cartService;
        }

        // ── helper: lấy UserId từ Claims ──
        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy cart items để checkout:
        /// - Ưu tiên đọc từ REORDER_KEY nếu đang ở luồng "Mua lại" (không đụng giỏ hàng thật)
        /// - Nếu không, đọc từ giỏ hàng thật trong DB / Session
        /// </summary>
        private async Task<List<CartItem>> GetCartItemsAsync()
        {
            // Ưu tiên 1: Luồng Reorder — đọc từ Session riêng, không ảnh hưởng giỏ hàng thật
            var reorderItems = HttpContext.Session.Get<List<CartItem>>(REORDER_KEY);
            if (reorderItems != null && reorderItems.Any())
                return reorderItems;

            // Ưu tiên 2: Giỏ hàng thật (login hoặc guest)
            var userId = GetUserId();
            if (userId != null)
                return await _cartService.GetCartItemsAsync(userId.Value);

            return HttpContext.Session.Get<List<CartItem>>(CartController.CART_KEY) ?? new List<CartItem>();
        }

        // GET: /Checkout
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await GetCartItemsAsync();
            if (!cart.Any())
            {
                TempData["UserError"] = "Giỏ hàng của bạn đang trống! Hãy chọn ít nhất 1 sản phẩm.";
                return RedirectToAction("Index", "Cart");
            }

            // Lọc theo danh sách sản phẩm đã chọn (Session SELECTED_KEY vẫn dùng)
            var selectedIds = HttpContext.Session.Get<List<int>>(CartController.SELECTED_KEY);
            var checkoutItems = (selectedIds != null && selectedIds.Any())
                ? cart.Where(c => selectedIds.Contains(c.ProductId)).ToList()
                : cart;

            if (!checkoutItems.Any())
            {
                TempData["UserError"] = "Không tìm thấy sản phẩm đã chọn. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }

            return View(new CheckoutViewModel { CartItems = checkoutItems, ShippingFee = 30000 });
        }

        // POST: /Checkout/UpdateQuantity (dùng trong trang checkout)
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                var warning = await _cartService.UpdateQuantityAsync(userId.Value, productId, quantity);
                if (warning != null) TempData["StockWarning"] = warning;
            }
            else
            {
                // Fallback session
                var cart = HttpContext.Session.Get<List<CartItem>>(CartController.CART_KEY) ?? new List<CartItem>();
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                {
                    if (quantity <= 0) cart.Remove(item);
                    else item.Quantity = quantity;
                    HttpContext.Session.Set(CartController.CART_KEY, cart);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Checkout/ApplyCoupon?code=XXX  (AJAX)
        [HttpGet]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Vui lòng nhập mã." });

            var cart = await GetCartItemsAsync();
            if (!cart.Any())
                return Json(new { success = false, message = "Giỏ hàng trống." });

            decimal subTotal    = cart.Sum(c => c.Price * c.Quantity);
            decimal shippingFee = 30000;
            int? userId         = GetUserId();

            var result = await _couponService.ValidateCouponAsync(code, subTotal, shippingFee, userId);
            if (!result.IsValid)
                return Json(new { success = false, message = result.Message });

            return Json(new
            {
                success          = true,
                discount         = result.DiscountAmount,
                shippingDiscount = result.ShippingDiscount,
                message          = result.Message
            });
        }

        // POST: /Checkout/PlaceOrder
        // ─── KIẾN TRÚC MỚI: Bất đồng bộ qua In-Memory Channel ───────────────────────
        // Controller chỉ làm 2 việc:
        //   1. Xác thực đầu vào (Coupon, giỏ hàng) — nhanh, không tốn tài nguyên
        //   2. Đẩy đơn hàng vào hàng đợi (OrderChannel) và trả về JSON ngay
        // Việc trừ kho + lưu DB được xử lý bởi OrderProcessingBackgroundService.
        // Kết quả thành công/thất bại sẽ được bắn về User qua SignalR (event "OrderPlacedResult").
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            [FromServices] IOrderChannel orderChannel,
            CheckoutViewModel model)
        {
            var cart = await GetCartItemsAsync();
            if (!cart.Any())
                return Json(new { success = false, message = "Giỏ hàng trống! Đơn hàng không hợp lệ." });

            model.CartItems   = cart;
            model.ShippingFee = 30000;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();
                return Json(new { success = false, message = errors ?? "Thông tin đặt hàng không hợp lệ." });
            }

            var userId = GetUserId();
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập để đặt hàng." });

            // ── Tái xác thực Coupon ở Backend để chống giả mạo ──────────────────────
            if (!string.IsNullOrEmpty(model.CouponCode))
            {
                var result = await _couponService.ValidateCouponAsync(
                    model.CouponCode, model.SubTotal, model.ShippingFee, userId);

                if (result.IsValid)
                {
                    model.DiscountAmount = result.DiscountAmount;
                    // Điều chỉnh phí ship nếu là FreeShipping coupon
                    if (result.ShippingDiscount > 0)
                        model.ShippingFee = Math.Max(0, model.ShippingFee - result.ShippingDiscount);
                }
                else
                {
                    model.DiscountAmount = 0; // Coupon không hợp lệ ở backend → bỏ qua discount
                }
            }

            // ── Đóng gói Request ─────────────────────────────────────────
            var request = new OrderRequestDto
            {
                UserId          = userId.Value,
                ReceiverName    = model.ReceiverName,
                ReceiverPhone   = model.ReceiverPhone,
                ShippingAddress = model.ShippingAddress,
                Province        = model.Province,
                PaymentMethod   = model.PaymentMethod,
                CouponCode      = model.CouponCode,
                Note            = model.Note,
                ShippingFee     = model.ShippingFee,
                DiscountAmount  = model.DiscountAmount,
                SubTotal        = model.SubTotal,
                FinalAmount     = model.FinalTotal,
                Items           = cart
            };

            // ── Phân nhánh xử lý theo Phương thức thanh toán ─────────────
            if (model.PaymentMethod == TechGearShop_V1.Models.Entities.PaymentMethod.VNPay)
            {
                try
                {
                    // 1. Lưu DB ngay lập tức (không qua Channel) để lấy OrderId
                    int orderId = await _orderService.CreatePendingPaymentOrderAsync(request);

                    // 2. Xóa giỏ hàng
                    await ClearCartSessionAndDbAsync(userId.Value);

                    // 3. Trả về link Redirect sang VNPay
                    return Json(new
                    {
                        success = true,
                        redirect = $"/VNPay/Pay?orderId={orderId}"
                    });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            else
            {
                // COD: Đẩy vào Channel xử lý nền
                await orderChannel.WriteAsync(request);
                await ClearCartSessionAndDbAsync(userId.Value);

                return Json(new
                {
                    success = true,
                    queued  = true,
                    message = "Hệ thống đang xử lý đơn hàng của bạn. Vui lòng chờ trong giây lát..."
                });
            }
        }

        private async Task ClearCartSessionAndDbAsync(int userId)
        {
            var isReorder = HttpContext.Session.Get<List<CartItem>>(REORDER_KEY) != null;
            if (isReorder)
            {
                HttpContext.Session.Remove(REORDER_KEY);
            }
            else
            {
                await _cartService.ClearCartAsync(userId);
                HttpContext.Session.Remove(CartController.CART_KEY);
                HttpContext.Session.Remove(CartController.SELECTED_KEY);
            }
        }

        public IActionResult Success(int orderId) => View(orderId);
    }
}
