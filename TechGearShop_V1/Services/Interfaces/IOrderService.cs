using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IOrderService
    {
        Task<bool> PlaceOrderAsync(Order order);
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task<IEnumerable<Order>> GetAllOrdersWithUsersAsync();
        Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedOrdersAsync(string keyword, OrderStatus? status, int page, int pageSize);
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
        Task<bool> CancelOrderAsync(int orderId, int userId);
    }
}
