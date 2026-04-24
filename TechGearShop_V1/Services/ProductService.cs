using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IImageService _imageService;

        public ProductService(IProductRepository productRepository, IImageService imageService)
        {
            _productRepository = productRepository;
            _imageService = imageService;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetProductWithCategoryAsync(id);
        }

        public async Task CreateProductAsync(Product product)
        {
            await _productRepository.AddAsync(product);
            await _productRepository.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                product.IsActive = false; // Soft delete
                _productRepository.Update(product);
                await _productRepository.SaveChangesAsync();
            }
        }

        public async Task DeleteProductImageAsync(int imageId)
        {
            // Lấy thông tin ảnh trước khi xóa
            var image = await _productRepository.GetProductImageByIdAsync(imageId);
            
            // Xóa ảnh trên mây (Hard delete)
            if (image != null && !string.IsNullOrEmpty(image.PublicId))
            {
                await _imageService.DeleteImageByPublicIdAsync(image.PublicId);
            }
            
            // Xóa ảnh trong DB
            await _productRepository.DeleteProductImageAsync(imageId);
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count)
        {
            return await _productRepository.GetFeaturedProductsAsync(count);
        }

        public async Task<IEnumerable<Product>> GetNewProductsAsync(int count)
        {
            return await _productRepository.GetNewProductsAsync(count);
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _productRepository.GetProductsByCategoryIdAsync(categoryId);
        }

        public async Task<IEnumerable<Product>> FilterProductsAsync(int? categoryId, string? keyword, string? sortOrder)
        {
            return await _productRepository.FilterProductsAsync(categoryId, keyword, sortOrder);
        }
    }
}
