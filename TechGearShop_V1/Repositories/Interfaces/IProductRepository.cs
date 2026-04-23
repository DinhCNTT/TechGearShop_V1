using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Repositories.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count);
        Task<Product?> GetProductWithCategoryAsync(int id);
        Task<IEnumerable<Product>> FilterProductsAsync(int? categoryId, string? keyword, string? sortOrder);
    }
}
