using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IImageService _imageService;
        private readonly IStockNotificationQueue _notificationQueue;

        public ProductController(IProductService productService, ICategoryService categoryService, IImageService imageService, IStockNotificationQueue notificationQueue)
        {
            _productService = productService;
            _categoryService = categoryService;
            _imageService = imageService;
            _notificationQueue = notificationQueue;
        }

        public async Task<IActionResult> Index(string searchKeyword, int? categoryId, int page = 1)
        {
            const int pageSize = 10;
            var allProducts = await _productService.GetAllProductsAsync(); // Có thể tối ưu lấy IQueryable nếu cần hiệu năng cực cao, tạm thời dùng LINQ to Objects
            
            // Lọc dữ liệu
            var query = allProducts.AsEnumerable();
            
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var lowerKw = searchKeyword.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lowerKw) || (p.Brand != null && p.Brand.ToLower().Contains(lowerKw)));
            }

            var totalItems = query.Count();

            // Phân trang
            var pagedProducts = query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var model = new ProductListViewModel
            {
                Products = pagedProducts,
                Categories = await _categoryService.GetAllCategoriesAsync(),
                CurrentCategoryId = categoryId,
                CurrentKeyword = searchKeyword,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var model = new ProductCreateViewModel
            {
                Categories = await _categoryService.GetAllCategoriesAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? imagePath = null;
                if (model.ImageFile != null)
                {
                    // Upload ảnh và watermark
                    imagePath = await _imageService.UploadImageWithWatermarkAsync(model.ImageFile, "products");
                }

                var product = new Product
                {
                    Name = model.Name,
                    Brand = model.Brand,
                    CategoryId = model.CategoryId,
                    Price = model.Price,
                    CostPrice = model.CostPrice,
                    PromotionalPrice = model.PromotionalPrice,
                    Stock = model.Stock,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    IsFeatured = model.IsFeatured,
                    ImagePath = imagePath
                };

                // Xử lý Gallery Files
                if (model.GalleryFiles != null && model.GalleryFiles.Any())
                {
                    var uploadResults = await _imageService.UploadMultipleImagesAsync(model.GalleryFiles, "products");
                    foreach (var result in uploadResults)
                    {
                        product.ProductImages.Add(new ProductImage
                        {
                            ImageUrl = result.Url,
                            PublicId = result.PublicId,
                            SortOrder = product.ProductImages.Count
                        });
                    }
                }

                await _productService.CreateProductAsync(product);
                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            // Nếu lỗi validation, load lại danh mục
            model.Categories = await _categoryService.GetAllCategoriesAsync();
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var model = new ProductCreateViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Brand = product.Brand ?? "",
                CategoryId = product.CategoryId,
                Price = product.Price,
                CostPrice = product.CostPrice,
                PromotionalPrice = product.PromotionalPrice,
                Stock = product.Stock,
                Description = product.Description,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                ExistingImagePath = product.ImagePath,
                ExistingGallery = product.ProductImages?.OrderBy(p => p.SortOrder).ToList() ?? new List<ProductImage>(),
                Categories = await _categoryService.GetAllCategoriesAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = await _productService.GetProductByIdAsync(model.Id);
                if (product == null) return NotFound();

                // Nếu có upload ảnh mới
                if (model.ImageFile != null)
                {
                    product.ImagePath = await _imageService.UploadImageWithWatermarkAsync(model.ImageFile, "products");
                }

                int oldStock = product.Stock;

                product.Name = model.Name;
                product.Brand = model.Brand;
                product.CategoryId = model.CategoryId;
                product.Price = model.Price;
                product.CostPrice = model.CostPrice;
                product.PromotionalPrice = model.PromotionalPrice;
                product.Stock = model.Stock;
                product.Description = model.Description;
                product.IsActive = model.IsActive;
                product.IsFeatured = model.IsFeatured;

                if (model.GalleryFiles != null && model.GalleryFiles.Any())
                {
                    var uploadResults = await _imageService.UploadMultipleImagesAsync(model.GalleryFiles, "products");
                    foreach (var result in uploadResults)
                    {
                        product.ProductImages.Add(new ProductImage
                        {
                            ImageUrl = result.Url,
                            PublicId = result.PublicId,
                            SortOrder = product.ProductImages.Count
                        });
                    }
                }

                await _productService.UpdateProductAsync(product);

                // Nếu nhập hàng từ hết hàng (0) sang có hàng (> 0), kích hoạt luồng tự động gửi thông báo ngầm
                if (oldStock == 0 && product.Stock > 0)
                {
                    await _notificationQueue.QueueRestockNotificationAsync(product.Id);
                }

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            model.Categories = await _categoryService.GetAllCategoriesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            TempData["SuccessMessage"] = "Sản phẩm đã chuyển sang trạng thái Ngừng kinh doanh (Soft Delete).";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DeleteGalleryImage(int imageId, int productId)
        {
            await _productService.DeleteProductImageAsync(imageId);
            TempData["SuccessMessage"] = "Đã xóa ảnh khỏi thư viện.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }
    }
}
