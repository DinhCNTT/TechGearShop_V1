using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Repositories.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IEnumerable<Product>> GetAllWithCategoryAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count);
        Task<IEnumerable<Product>> GetNewProductsAsync(int count);
        Task<Product?> GetProductWithCategoryAsync(int id);
        Task<ProductImage?> GetProductImageByIdAsync(int imageId);
        Task DeleteProductImageAsync(int imageId);
        Task<(IEnumerable<Product> Products, int TotalItems)> FilterProductsAsync(int? categoryId, string? keyword, string? sortOrder, int page = 1, int pageSize = 12, decimal? minPrice = null, decimal? maxPrice = null);
    }
}
