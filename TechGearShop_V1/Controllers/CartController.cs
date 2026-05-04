using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechGearShop_V1.Extensions;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class CartController : Controller
    {
        // Dùng lại key cũ để các chỗ khác (Checkout) đọc Session vẫn hoạt động
        public const string CART_KEY     = "shopping_cart";
        public const string SELECTED_KEY = "selected_cart_items";

        private readonly ICartService    _cartService;
        private readonly IProductService _productService;
        private readonly IOrderService   _orderService;

        public CartController(ICartService cartService, IProductService productService, IOrderService orderService)
        {
            _cartService    = cartService;
            _productService = productService;
            _orderService   = orderService;
        }

        // ── Helper: lấy UserId từ Claims ──
        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        // ── Helper tương thích: trả về CartItem list (dùng bởi các chỗ ngoài controller) ──
        public async Task<List<CartItem>> GetCartItemsAsync()
        {
            var userId = GetUserId();
            if (userId == null) return new List<CartItem>();
            return await _cartService.GetCartItemsAsync(userId.Value);
        }

        // GET /Cart
        public async Task<IActionResult> Index()
        {
            if (!User.Identity!.IsAuthenticated)
                return Redirect("/Account/Login?ReturnUrl=/Cart");

            var userId = GetUserId()!.Value;
            var items  = await _cartService.GetCartItemsAsync(userId);
            return View(items);
        }

        // POST /Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            if (!User.Identity!.IsAuthenticated)
                return Json(new { success = false, requireLogin = true });

            var userId = GetUserId()!.Value;
            var (success, message, cartCount) = await _cartService.AddItemAsync(userId, productId, quantity);

            return Json(new { success, message, cartCount });
        }

        // POST /Cart/BuyNow — thêm vào giỏ rồi redirect thẳng đến Checkout
        [HttpPost]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                TempData["UserError"] = "Sếp vui lòng đăng nhập để Mua Ngay nhé.";
                return Redirect($"/Account/Login?ReturnUrl=/Product/Detail/{productId}");
            }

            var userId  = GetUserId()!.Value;
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || !product.IsActive)
                return RedirectToAction("Index", "Home");

            await _cartService.AddItemAsync(userId, productId, Math.Min(quantity, product.Stock));

            // Đánh dấu chỉ sản phẩm này được chọn checkout
            HttpContext.Session.Set(SELECTED_KEY, new List<int> { productId });
            return RedirectToAction("Index", "Checkout");
        }

        // POST /Cart/Remove (AJAX)
        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                await _cartService.RemoveItemAsync(userId.Value, productId);
                var newCount = await _cartService.GetCartCountAsync(userId.Value);
                return Json(new { success = true, newCount = newCount, message = "Đã bỏ sản phẩm khỏi giỏ hàng." });
            }
            return Json(new { success = false, message = "Vui lòng đăng nhập." });
        }

        // POST /Cart/UpdateQuantity (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                var warning = await _cartService.UpdateQuantityAsync(userId.Value, productId, quantity);
                var items = await _cartService.GetCartItemsAsync(userId.Value);
                var updatedItem = items.FirstOrDefault(i => i.ProductId == productId);
                
                return Json(new { 
                    success = true, 
                    warning = warning, 
                    newQuantity = updatedItem?.Quantity ?? 0, 
                    newPriceTotal = updatedItem != null ? updatedItem.Quantity * updatedItem.Price : 0,
                    newCount = items.Count // Tổng số môn (loại) SP
                });
            }
            return Json(new { success = false });
        }

        // POST /Cart/Clear
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var userId = GetUserId();
            if (userId != null)
                await _cartService.ClearCartAsync(userId.Value);

            HttpContext.Session.Remove(SELECTED_KEY);
            TempData["UserSuccess"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Cart/Checkout — lưu danh sách selected rồi redirect sang trang thanh toán
        [HttpPost]
        public IActionResult Checkout(List<int> selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any())
            {
                TempData["UserError"] = "Vui lòng chọn ít nhất 1 sản phẩm để thanh toán!";
                return RedirectToAction(nameof(Index));
            }
            HttpContext.Session.Set(SELECTED_KEY, selectedItems);
            return Redirect("/Checkout");
        }

        // POST /Cart/Reorder
        [HttpPost]
        public async Task<IActionResult> Reorder(int orderId)
        {
            if (!User.Identity!.IsAuthenticated)
                return Redirect("/Account/Login");

            var userId = GetUserId()!.Value;
            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            
            if (order == null || order.UserId != userId)
            {
                TempData["UserError"] = "Đơn hàng không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction("Orders", "Account");
            }

            // ✅ Xây dựng danh sách CartItem tạm từ đơn hàng cũ
            // Không thêm vào giỏ hàng thật — giỏ hàng cũ hoàn toàn không bị ảnh hưởng
            var reorderItems = new List<CartItem>();
            var hasOOSItems = false;

            foreach (var detail in order.OrderDetails)
            {
                var product = await _productService.GetProductByIdAsync(detail.ProductId);
                if (product != null && product.IsActive && product.Stock > 0)
                {
                    reorderItems.Add(new CartItem
                    {
                        ProductId   = product.Id,
                        ProductName = product.Name,
                        ImagePath   = product.ImagePath,
                        Price       = product.PromotionalPrice ?? product.Price,
                        Quantity    = Math.Min(detail.Quantity, product.Stock),
                        IsOutOfStock = false
                    });
                }
                else
                {
                    hasOOSItems = true;
                }
            }

            if (!reorderItems.Any())
            {
                TempData["UserError"] = "Tất cả sản phẩm trong đơn hàng này đã hết hàng hoặc ngừng kinh doanh.";
                return RedirectToAction("Orders", "Account");
            }

            // Lưu vào Session riêng — CheckoutController sẽ đọc từ đây thay vì giỏ thật
            HttpContext.Session.Set(CheckoutController.REORDER_KEY, reorderItems);

            if (hasOOSItems)
                TempData["UserSuccess"] = "Một số sản phẩm đã hết hàng nên bị loại bỏ. Đơn hàng mới đã được chuẩn bị!";

            // Redirect thẳng đến Checkout — giỏ hàng cũ KHÔNG bị thay đổi
            return Redirect("/Checkout");
        }
    }
}
