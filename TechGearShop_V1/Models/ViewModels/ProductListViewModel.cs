using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Models.ViewModels
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        
        // Trạng thái Filter đang chọn để binding lại lên View
        public int? CurrentCategoryId { get; set; }
        public string? CurrentKeyword { get; set; }
        public string? CurrentSortOrder { get; set; }
        // Pagination cho Admin
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
