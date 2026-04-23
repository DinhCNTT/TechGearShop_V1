using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Extensions;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICouponService _couponService;
        private readonly IProductService _productService; // For double-checking stock before saving

        public CheckoutController(IOrderService orderService, ICouponService couponService, IProductService productService)
        {
            _orderService = orderService;
            _couponService = couponService;
            _productService = productService;
        }

        private List<CartItem> GetCartItems()
        {
            return HttpContext.Session.Get<List<CartItem>>(CartController.CART_KEY) ?? new List<CartItem>();
        }

        // GET: /Checkout
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCartItems();
            if (!cart.Any())
            {
                TempData["UserError"] = "Giỏ hàng của bạn đang trống! Hãy chọn ít nhất 1 sản phẩm.";
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                // Giả lập logic tính phí ship cơ bản (VD: Cố định 30k)
                ShippingFee = 30000
            };

            return View(model);
        }

        // GET: /Checkout/ApplyCoupon?code=XXX
        // API for AJAX validation
        [HttpGet]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Vui lòng nhập mã." });

            var cart = GetCartItems();
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

                // Cap discount at subTotal to prevent negative final price
                if (discount > subTotal) discount = subTotal;

                return Json(new { success = true, discount = discount, message = "Áp dụng mã thành công!" });
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
            var cart = GetCartItems();
            if (!cart.Any())
            {
                TempData["UserError"] = "Giỏ hàng trống! Đơn hàng không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }
            
            model.CartItems = cart;
            model.ShippingFee = 30000; // Static simulation

            if (!ModelState.IsValid)
            {
                // Reload form with errors
                return View("Index", model);
            }

            try
            {
                // 1. Tái xác thực Coupon an toàn từ Backend để chống giả mạo POST request
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

                // 2. Chốt chặn Stock chặt chẽ cuối cùng ngay trước khi Build Order Model
                foreach (var item in cart)
                {
                    var product = await _productService.GetProductByIdAsync(item.ProductId);
                    if (product == null || product.Stock < item.Quantity)
                    {
                        ModelState.AddModelError("", $"Sản phẩm '{item.ProductName}' đã hết hàng hoặc không đủ số lượng.");
                        return View("Index", model);
                    }
                }

                // 3. Xây dựng Object Order Mapping
                var order = new Order
                {
                    // TODO: Thay bằng Id User thật từ JWT/Identity sau này (Mặc định lấy 1 để Database không lỗi)
                    UserId = 1, 
                    ReceiverName = model.ReceiverName,
                    ReceiverPhone = model.ReceiverPhone,
                    ShippingAddress = model.ShippingAddress,
                    Province = model.Province,
                    PaymentMethod = model.PaymentMethod,
                    CouponCode = model.CouponCode,
                    ShippingFee = model.ShippingFee,
                    DiscountAmount = model.DiscountAmount,
                    TotalAmount = model.SubTotal,
                    FinalAmount = model.FinalTotal,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    OrderDetails = cart.Select(c => new OrderDetail
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        UnitPrice = c.Price
                    }).ToList()
                };

                // 4. Bơm qua Service xử lý DB Transaction (Atomicity)
                bool isSuccess = await _orderService.PlaceOrderAsync(order);
                if (isSuccess)
                {
                    // Xóa giỏ hàng Session
                    HttpContext.Session.Remove(CartController.CART_KEY);
                    
                    return RedirectToAction(nameof(Success), new { orderId = order.Id });
                }
                else
                {
                    ModelState.AddModelError("", "Rất tiếc! Đã xảy ra lỗi trong quá trình xử lý đơn hàng. Vui lòng thử lại.");
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View("Index", model);
            }
        }

        public IActionResult Success(int orderId)
        {
            return View(orderId);
        }
    }
}
