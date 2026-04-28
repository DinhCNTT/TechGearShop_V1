using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Repositories.Interfaces;

namespace TechGearShop_V1.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        // ── private helper: lấy hoặc tạo Cart cho user ──
        private async Task<CartEntity> GetOrCreateCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new CartEntity { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<CartEntity?> GetCartByUserIdAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<CartItemEntity> AddOrUpdateItemAsync(int userId, int productId, int quantity, decimal unitPrice)
        {
            var cart = await GetOrCreateCartAsync(userId);

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.Quantity += quantity;
                item.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                item = new CartItemEntity
                {
                    CartId    = cart.Id,
                    ProductId = productId,
                    Quantity  = quantity,
                    UnitPrice = unitPrice
                };
                _context.CartItems.Add(item);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task RemoveItemAsync(int userId, int productId)
        {
            var item = await _context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i => i.Cart.UserId == userId && i.ProductId == productId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateItemQuantityAsync(int userId, int productId, int quantity)
        {
            var item = await _context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i => i.Cart.UserId == userId && i.ProductId == productId);

            if (item != null)
            {
                item.Quantity  = quantity;
                item.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(int userId)
        {
            var items = await _context.CartItems
                .Include(i => i.Cart)
                .Where(i => i.Cart.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task MergeSessionCartAsync(int userId, List<CartItem> sessionItems)
        {
            if (sessionItems == null || !sessionItems.Any()) return;

            var cart = await GetOrCreateCartAsync(userId);

            foreach (var si in sessionItems)
            {
                var existing = cart.Items.FirstOrDefault(i => i.ProductId == si.ProductId);
                if (existing != null)
                {
                    // Cộng dồn số lượng khi merge
                    existing.Quantity += si.Quantity;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.CartItems.Add(new CartItemEntity
                    {
                        CartId    = cart.Id,
                        ProductId = si.ProductId,
                        Quantity  = si.Quantity,
                        UnitPrice = si.Price
                    });
                }
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
