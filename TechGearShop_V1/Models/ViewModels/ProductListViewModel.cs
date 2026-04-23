using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Models.ViewModels
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }

        // Parameters for preserving filters in pagination links
        public string? SearchBrand { get; set; }
        public int? SearchCategoryId { get; set; }
    }
}
