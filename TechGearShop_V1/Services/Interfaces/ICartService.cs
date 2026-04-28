using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface ICartService
    {
        /// <summary>Lấy toàn bộ items trong giỏ của user (đã map sang CartItem ViewModel).</summary>
        Task<List<CartItem>> GetCartItemsAsync(int userId);

        /// <summary>Thêm sản phẩm vào giỏ. Trả về (success, message, newCartCount).</summary>
        Task<(bool Success, string Message, int CartCount)> AddItemAsync(int userId, int productId, int quantity);

        /// <summary>Xóa 1 sản phẩm khỏi giỏ.</summary>
        Task RemoveItemAsync(int userId, int productId);

        /// <summary>Cập nhật số lượng. Trả về message cảnh báo nếu vượt tồn kho.</summary>
        Task<string?> UpdateQuantityAsync(int userId, int productId, int quantity);

        /// <summary>Xóa toàn bộ giỏ hàng.</summary>
        Task ClearCartAsync(int userId);

        /// <summary>Merge giỏ hàng từ Session (guest) vào DB khi user đăng nhập.</summary>
        Task MergeSessionCartAsync(int userId, List<CartItem> sessionItems);

        /// <summary>Lấy tổng số item trong giỏ (dùng cho badge header).</summary>
        Task<int> GetCartCountAsync(int userId);
    }
}
