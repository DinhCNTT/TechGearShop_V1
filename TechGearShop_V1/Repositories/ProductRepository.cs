using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;

namespace TechGearShop_V1.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>Lấy toàn bộ sản phẩm kèm thông tin Danh mục (dùng cho Admin list).</summary>
        public async Task<IEnumerable<Product>> GetAllWithCategoryAsync()
        {
            return await _dbSet
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Category.IsActive)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.SoldCount)
                .ThenByDescending(p => p.ViewCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetNewProductsAsync(int count)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Category.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.IsActive && p.Category.IsActive)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithCategoryAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ProductImage?> GetProductImageByIdAsync(int imageId)
        {
            return await _context.ProductImages.FindAsync(imageId);
        }

        public async Task DeleteProductImageAsync(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image != null)
            {
                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(IEnumerable<Product> Products, int TotalItems)> FilterProductsAsync(int? categoryId, string? keyword, string? sortOrder, int page = 1, int pageSize = 12, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var query = _dbSet.Include(p => p.Category).Where(p => p.IsActive && p.Category.IsActive).AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lowerKeyword) || (p.Brand != null && p.Brand.ToLower().Contains(lowerKeyword)));
            }
            
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt) // Mặc định là Mới nhất
            };
            
            var totalItems = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (products, totalItems);
        }
    }
}
