using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;
        private readonly IProductService _productService;

        public CartService(ICartRepository cartRepo, IProductService productService)
        {
            _cartRepo       = cartRepo;
            _productService = productService;
        }

        public async Task<List<CartItem>> GetCartItemsAsync(int userId)
        {
            var cart = await _cartRepo.GetCartByUserIdAsync(userId);
            if (cart == null) return new List<CartItem>();

            var result = new List<CartItem>();
            foreach (var item in cart.Items)
            {
                // Refresh giá hiện tại từ DB để phát hiện chênh lệch
                var currentPrice = item.Product.PromotionalPrice ?? item.Product.Price;

                result.Add(new CartItem
                {
                    ProductId    = item.ProductId,
                    ProductName  = item.Product.Name,
                    ImagePath    = item.Product.ImagePath,
                    Price        = currentPrice,          // luôn hiện giá mới nhất
                    Quantity     = item.Quantity,
                    IsOutOfStock = item.Quantity > item.Product.Stock
                });
            }

            return result;
        }

        public async Task<(bool Success, string Message, int CartCount)> AddItemAsync(int userId, int productId, int quantity)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || !product.IsActive)
                return (false, "Sản phẩm không tồn tại hoặc ngừng kinh doanh.", 0);

            // Lấy giỏ hiện tại để kiểm tra tổng số lượng
            var currentItems = await GetCartItemsAsync(userId);
            var existingQty  = currentItems.FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;

            if (existingQty + quantity > product.Stock)
                return (false, $"Xin lỗi, kho chỉ còn {product.Stock} sản phẩm.", 0);

            var unitPrice = product.PromotionalPrice ?? product.Price;
            await _cartRepo.AddOrUpdateItemAsync(userId, productId, quantity, unitPrice);

            var count = await GetCartCountAsync(userId);
            return (true, $"Đã thêm {product.Name} vào giỏ!", count);
        }

        public async Task RemoveItemAsync(int userId, int productId)
            => await _cartRepo.RemoveItemAsync(userId, productId);

        public async Task<string?> UpdateQuantityAsync(int userId, int productId, int quantity)
        {
            if (quantity <= 0)
            {
                await _cartRepo.RemoveItemAsync(userId, productId);
                return null;
            }

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) return null;

            string? warning = null;
            if (quantity > product.Stock)
            {
                quantity = product.Stock;
                warning  = $"Kho chỉ còn tối đa {product.Stock} sản phẩm.";
            }

            await _cartRepo.UpdateItemQuantityAsync(userId, productId, quantity);
            return warning;
        }

        public async Task ClearCartAsync(int userId)
            => await _cartRepo.ClearCartAsync(userId);

        public async Task MergeSessionCartAsync(int userId, List<CartItem> sessionItems)
            => await _cartRepo.MergeSessionCartAsync(userId, sessionItems);

        public async Task<int> GetCartCountAsync(int userId)
        {
            var cart = await _cartRepo.GetCartByUserIdAsync(userId);
            return cart?.Items.Count ?? 0; // Đếm số loại sản phẩm, như Shopee/Tiki
        }
    }
}
