using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IImageService _imageService;

        public ProductController(IProductService productService, ICategoryService categoryService, IImageService imageService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _imageService = imageService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var allProducts = await _productService.GetAllProductsAsync(); // Note: Cần tối ưu query nếu data lớn
            
            // Phân trang đơn giản bằng LINQ (Skip, Take)
            var pagedProducts = allProducts
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var model = new ProductListViewModel
            {
                Products = pagedProducts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = allProducts.Count(),
                TotalPages = (int)Math.Ceiling(allProducts.Count() / (double)pageSize)
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

                product.Name = model.Name;
                product.Brand = model.Brand;
                product.CategoryId = model.CategoryId;
                product.Price = model.Price;
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
