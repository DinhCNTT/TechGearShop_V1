using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IOrderService
    {
        Task<bool> PlaceOrderAsync(Order order);
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task<IEnumerable<Order>> GetAllOrdersWithUsersAsync();
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
    }
}
