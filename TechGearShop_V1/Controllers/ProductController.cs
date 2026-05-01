using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IStockSubscriptionService _stockSubscriptionService;

        public ProductController(IProductService productService, ICategoryService categoryService, IStockSubscriptionService stockSubscriptionService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _stockSubscriptionService = stockSubscriptionService;
        }

        // GET: /Product?categoryId=1&keyword=abc&sortOrder=price_asc
        public async Task<IActionResult> Index(int? categoryId, string? keyword, string? sortOrder)
        {
            var products = await _productService.FilterProductsAsync(categoryId, keyword, sortOrder);
            var categories = await _categoryService.GetActiveCategoriesAsync();

            var model = new ProductListViewModel
            {
                Products = products,
                Categories = categories,
                CurrentCategoryId = categoryId,
                CurrentKeyword = keyword,
                CurrentSortOrder = sortOrder
            };

            return View(model);
        }

        // GET: /Product/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null || !product.IsActive || (product.Category != null && !product.Category.IsActive))
            {
                // Thay vì quăng lỗi trang trắng, trả về NotFound thân thiện
                TempData["UserError"] = "Sản phẩm này không tồn tại hoặc đã ngừng kinh doanh.";
                return RedirectToAction(nameof(Index));
            }

            // Non-blocking ViewCount: ghi vào bộ nhớ, background service flush vào DB mỗi 5 phút
            var viewedList = HttpContext.Session.GetString("ViewedProducts");
            var viewedIds = string.IsNullOrEmpty(viewedList)
                ? new List<int>()
                : viewedList.Split(',').Select(int.Parse).ToList();

            if (!viewedIds.Contains(id))
            {
                Services.ViewCountFlushService.PendingViews.AddOrUpdate(id, 1, (_, existing) => existing + 1);
                viewedIds.Add(id);
                HttpContext.Session.SetString("ViewedProducts", string.Join(",", viewedIds));
            }

            // Gợi ý sản phẩm cùng danh mục (loại trừ chính nó)
            var related = await _productService.GetProductsByCategoryAsync(product.CategoryId);
            ViewBag.RelatedProducts = related.Where(p => p.Id != product.Id).Take(4).ToList();

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> SubscribeStockNotification(int productId, string email)
        {
            int? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
                {
                    userId = uid;
                }
            }

            if (!userId.HasValue && string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Vui lòng nhập Email để nhận thông báo." });
            }

            var result = await _stockSubscriptionService.SubscribeAsync(productId, userId, email);
            
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
