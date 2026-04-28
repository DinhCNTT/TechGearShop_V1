using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechGearShop_V1.Extensions;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class CheckoutController : Controller
    {
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
        /// Lấy cart items từ DB (user đã login) hoặc Session (fallback cho guest).
        /// </summary>
        private async Task<List<CartItem>> GetCartItemsAsync()
        {
            var userId = GetUserId();
            if (userId != null)
                return await _cartService.GetCartItemsAsync(userId.Value);

            // Fallback: đọc từ Session nếu user chưa login
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

            decimal subTotal = cart.Sum(c => c.Price * c.Quantity);

            try
            {
                var coupon = await _couponService.GetValidCouponAsync(code, subTotal);
                if (coupon == null)
                    return Json(new { success = false, message = "Mã không hợp lệ hoặc hết hạn." });

                decimal discount = coupon.IsPercentage
                    ? (subTotal * coupon.DiscountValue) / 100
                    : coupon.DiscountValue;

                if (discount > subTotal) discount = subTotal;
                return Json(new { success = true, discount, message = "Áp dụng mã thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = await GetCartItemsAsync();
            if (!cart.Any())
            {
                TempData["UserError"] = "Giỏ hàng trống! Đơn hàng không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            model.CartItems = cart;
            model.ShippingFee = 30000;

            if (!ModelState.IsValid)
                return View("Index", model);

            try
            {
                // Tái xác thực Coupon ở Backend để chống giả mạo
                if (!string.IsNullOrEmpty(model.CouponCode))
                {
                    var coupon = await _couponService.GetValidCouponAsync(model.CouponCode, model.SubTotal);
                    if (coupon != null)
                    {
                        decimal calcDiscount = coupon.IsPercentage
                            ? (model.SubTotal * coupon.DiscountValue) / 100
                            : coupon.DiscountValue;
                        if (calcDiscount > model.SubTotal) calcDiscount = model.SubTotal;
                        model.DiscountAmount = calcDiscount;
                    }
                }

                // Chốt chặn Stock lần cuối trước khi lưu Order
                foreach (var item in cart)
                {
                    var product = await _productService.GetProductByIdAsync(item.ProductId);
                    if (product == null || product.Stock < item.Quantity)
                    {
                        ModelState.AddModelError("", $"Sản phẩm '{item.ProductName}' đã hết hàng hoặc không đủ số lượng.");
                        return View("Index", model);
                    }
                }

                // Lấy UserId thực từ Claims
                var userId = GetUserId() ?? 1;

                var order = new Order
                {
                    UserId          = userId,
                    ReceiverName    = model.ReceiverName,
                    ReceiverPhone   = model.ReceiverPhone,
                    ShippingAddress = model.ShippingAddress,
                    Province        = model.Province,
                    PaymentMethod   = model.PaymentMethod,
                    CouponCode      = model.CouponCode,
                    Note            = model.Note,
                    ShippingFee     = model.ShippingFee,
                    DiscountAmount  = model.DiscountAmount,
                    TotalAmount     = model.SubTotal,
                    FinalAmount     = model.FinalTotal,
                    OrderDate       = DateTime.UtcNow,
                    Status          = OrderStatus.Pending,
                    OrderDetails    = cart.Select(c => new OrderDetail
                    {
                        ProductId = c.ProductId,
                        Quantity  = c.Quantity,
                        UnitPrice = c.Price
                    }).ToList()
                };

                bool isSuccess = await _orderService.PlaceOrderAsync(order);
                if (isSuccess)
                {
                    // Xóa giỏ hàng sau khi đặt hàng thành công
                    var uid = GetUserId();
                    if (uid != null)
                        await _cartService.ClearCartAsync(uid.Value);          // DB
                    HttpContext.Session.Remove(CartController.CART_KEY);       // Session fallback
                    HttpContext.Session.Remove(CartController.SELECTED_KEY);

                    return RedirectToAction(nameof(Success), new { orderId = order.Id });
                }

                ModelState.AddModelError("", "Rất tiếc! Đã xảy ra lỗi trong quá trình xử lý đơn hàng. Vui lòng thử lại.");
                return View("Index", model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View("Index", model);
            }
        }

        public IActionResult Success(int orderId) => View(orderId);
    }
}
