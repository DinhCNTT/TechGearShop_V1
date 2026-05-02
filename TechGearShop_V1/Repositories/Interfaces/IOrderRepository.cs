using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId);
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
        // Transaction for checkout
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<IEnumerable<Order>> GetAllOrdersWithUsersAsync();
        Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedOrdersAsync(string keyword, OrderStatus? status, int page, int pageSize);
    }
}
