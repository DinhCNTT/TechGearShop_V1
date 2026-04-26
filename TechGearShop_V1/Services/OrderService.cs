using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserService _userService;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IUserService userService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userService = userService;
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
        {
            return await _orderRepository.GetOrdersByUserIdAsync(userId);
        }

        public async Task<bool> PlaceOrderAsync(Order order)
        {
            try
            {
                await _orderRepository.BeginTransactionAsync();

                // 1. Thêm order vào DB
                await _orderRepository.AddAsync(order);
                await _orderRepository.SaveChangesAsync();

                // 2. Cập nhật lại số lượng tồn kho (Stock)
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _productRepository.GetByIdAsync(detail.ProductId);
                    if (product != null)
                    {
                        if (product.Stock < detail.Quantity)
                        {
                            throw new Exception($"Sản phẩm {product.Name} không đủ số lượng tồn kho.");
                        }
                        product.Stock -= detail.Quantity;
                        _productRepository.Update(product);
                    }
                }
                await _productRepository.SaveChangesAsync();

                await _orderRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                await _orderRepository.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                _orderRepository.Update(order);
                await _orderRepository.SaveChangesAsync();

                // Bonus logic: Nếu đơn hàng hoàn thành -> cộng điểm thưởng cho User và tăng lượt bán
                if (status == OrderStatus.Completed)
                {
                    // Cộng điểm (VD: 1 điểm cho mỗi 100,000 VND)
                    int pointsEarned = (int)(order.FinalAmount / 100000);
                    await _userService.UpdateUserPointsAsync(order.UserId, pointsEarned);

                    // Tăng số lượng đã bán (SoldCount) cho từng sản phẩm
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = await _productRepository.GetByIdAsync(detail.ProductId);
                        if (product != null)
                        {
                            product.SoldCount += detail.Quantity;
                            _productRepository.Update(product);
                        }
                    }
                    await _productRepository.SaveChangesAsync();
                }
            }
        }

        public async Task<IEnumerable<Order>> GetAllOrdersWithUsersAsync()
        {
            return await _orderRepository.GetAllOrdersWithUsersAsync();
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _orderRepository.GetOrderWithDetailsAsync(orderId);
        }
    }
}
