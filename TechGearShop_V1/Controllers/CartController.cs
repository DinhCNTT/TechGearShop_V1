using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Extensions;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class CartController : Controller
    {
        public const string CART_KEY = "shopping_cart";
        public const string SELECTED_KEY = "selected_cart_items";
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
        public async Task<IActionResult> Add(int productId, int quantity = 1)
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
                if (item.Quantity + quantity > product.Stock)
                {
                    return Json(new { success = false, message = $"Xin lỗi, kho chỉ còn {product.Stock} sản phẩm." });
                }
                item.Quantity += quantity;
            }
            else
            {
                if (product.Stock < quantity)
                {
                    return Json(new { success = false, message = $"Sản phẩm hiện đang tạm hết hoặc không đủ {quantity} món." });
                }
                
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.PromotionalPrice ?? product.Price,
                    ImagePath = product.ImagePath,
                    Quantity = quantity
                });
            }

            SaveCartSession(cart);
            int totalCount = cart.Count;
            
            // 2. Trả về JSON để Frontend xử lý AJAX (không load lại trang)
            return Json(new { success = true, message = $"Đã thêm {product.Name} vào giỏ!", cartCount = totalCount });
        }

        // Action MUA NGAY: Thêm vào giỏ và chuyển thẳng tới trang Checkout (áp dụng ngay vào SelectedSession)
        [HttpPost]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || !product.IsActive)
                return RedirectToAction("Index", "Home");

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                if (item.Quantity + quantity <= product.Stock) item.Quantity += quantity;
                else item.Quantity = product.Stock; // Max out if over
            }
            else
            {
                if (product.Stock >= quantity)
                {
                    cart.Add(new CartItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Price = product.PromotionalPrice ?? product.Price,
                        ImagePath = product.ImagePath,
                        Quantity = quantity
                    });
                }
            }
            SaveCartSession(cart);
            
            // Thiết lập Session mảng SelectedItems chỉ chứa ID của sản phẩm này
            HttpContext.Session.Set(SELECTED_KEY, new List<int> { productId });

            return RedirectToAction("Index", "Checkout");
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
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return Remove(productId);
            }

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product != null)
                {
                    if (quantity > product.Stock)
                    {
                        TempData["StockWarning"] = $"Khối lượng trong kho chỉ còn tối đa {product.Stock} sản phẩm.";
                        item.Quantity = product.Stock;
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }
                    SaveCartSession(cart);
                }
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_KEY);
            HttpContext.Session.Remove(SELECTED_KEY); // Xóa cả selection
            TempData["UserSuccess"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        // Lưu danh sách sản phẩm đã chọn vào Session, rồi redirect sang Checkout
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
    }
}
