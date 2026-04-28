using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;

namespace TechGearShop_V1.Repositories.Interfaces
{
    public interface ICartRepository
    {
        /// <summary>Lấy cart của user (kèm items + product info). Trả về null nếu chưa có.</summary>
        Task<CartEntity?> GetCartByUserIdAsync(int userId);

        /// <summary>Thêm mới hoặc cập nhật số lượng item trong giỏ. Tự tạo Cart nếu chưa có.</summary>
        Task<CartItemEntity> AddOrUpdateItemAsync(int userId, int productId, int quantity, decimal unitPrice);

        /// <summary>Xóa một item khỏi giỏ.</summary>
        Task RemoveItemAsync(int userId, int productId);

        /// <summary>Cập nhật số lượng item.</summary>
        Task UpdateItemQuantityAsync(int userId, int productId, int quantity);

        /// <summary>Xóa toàn bộ items trong giỏ (dùng sau checkout).</summary>
        Task ClearCartAsync(int userId);

        /// <summary>Merge danh sách CartItem từ Session vào DB khi user đăng nhập.</summary>
        Task MergeSessionCartAsync(int userId, List<CartItem> sessionItems);
    }
}
