using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TechGearShop_V1.Models;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public HomeController(ILogger<HomeController> logger, IProductService productService, ICategoryService categoryService)
    {
        _logger = logger;
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index()
    {
        // View Model tổng hợp dữ liệu Trang chủ
        var vm = new HomeViewModel();

        // 1. Quét danh mục sản phẩm (sửa dụng Cache sau này)
        vm.Categories = await _categoryService.GetAllCategoriesAsync();

        // 2. Sản phẩm nổi bật (Lọc 8 sản phẩm Featured)
        vm.FeaturedProducts = await _productService.GetFeaturedProductsAsync(8);

        // 3. Sản phẩm mới (Quét toàn bộ rồi OrderByDescending lấy 4-8 cái mới nhất)
        // Cách nhanh: dùng GetAll, sau này nếu database lớn nên viết riêng method ở Repository
        var allProducts = await _productService.GetAllProductsAsync();
        vm.NewProducts = allProducts.OrderByDescending(p => p.CreatedAt).Take(8);

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
