using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Extensions;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class CartController : Controller
    {
        public const string CART_KEY = "shopping_cart";
        private readonly IProductService _productService;

        public CartController(IProductService productService)
        {
            _productService = productService;
        }

        public List<CartItem> GetCartItems()
        {
            return HttpContext.Session.Get<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
        }

        private void SaveCartSession(List<CartItem> cart)
        {
            HttpContext.Session.Set(CART_KEY, cart);
        }

        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || !product.IsActive)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc ngừng kinh doanh." });
            }

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            // 1. Chốt chặn tồn kho: Kiểm tra số lượng trước khi thêm
            if (item != null)
            {
                if (item.Quantity + 1 > product.Stock)
                {
                    return Json(new { success = false, message = $"Xin lỗi, kho chỉ còn {product.Stock} sản phẩm." });
                }
                item.Quantity++;
            }
            else
            {
                if (product.Stock < 1)
                {
                    return Json(new { success = false, message = "Sản phẩm hiện đang tạm hết hàng." });
                }
                
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.PromotionalPrice ?? product.Price,
                    ImagePath = product.ImagePath,
                    Quantity = 1
                });
            }

            SaveCartSession(cart);
            int totalCount = cart.Sum(c => c.Quantity);
            
            // 2. Trả về JSON để Frontend xử lý AJAX (không load lại trang)
            return Json(new { success = true, message = $"Đã thêm {product.Name} vào giỏ!", cartCount = totalCount });
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartSession(cart);
                TempData["UserSuccess"] = "Đã bỏ sản phẩm khỏi giỏ hàng.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return Remove(productId);
            }

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCartSession(cart);
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_KEY);
            TempData["UserSuccess"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
