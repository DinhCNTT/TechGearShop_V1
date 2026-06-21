using Moq;
using Microsoft.AspNetCore.SignalR;
using TechGearShop_V1.Hubs;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.Enums;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services;
using TechGearShop_V1.Services.Interfaces;
using Xunit;

namespace TechGearShop_V1.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepo;
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockHubClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _mockOrderRepo = new Mock<IOrderRepository>();
            _mockProductRepo = new Mock<IProductRepository>();
            _mockUserService = new Mock<IUserService>();
            _mockNotificationService = new Mock<INotificationService>();
            
            // Setup SignalR Hub Mocks
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockHubClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();
            
            _mockHubContext.Setup(h => h.Clients).Returns(_mockHubClients.Object);
            _mockHubClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
                            .Returns(Task.CompletedTask);

            // Mock Transactions
            _mockOrderRepo.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockOrderRepo.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockOrderRepo.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _orderService = new OrderService(
                _mockOrderRepo.Object,
                _mockProductRepo.Object,
                _mockUserService.Object,
                _mockNotificationService.Object,
                _mockHubContext.Object
            );
        }

        [Fact]
        public async Task PlaceOrderAsync_ReturnsTrue_WhenStockIsSufficient()
        {
            // Arrange
            var product1 = new Product { Id = 101, Name = "Laptop", Stock = 5 };
            var product2 = new Product { Id = 102, Name = "Mouse", Stock = 10 };

            var order = new Order
            {
                Id = 1,
                UserId = 10,
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { ProductId = 101, Quantity = 2 },
                    new OrderDetail { ProductId = 102, Quantity = 5 }
                }
            };

            _mockProductRepo.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(product1);
            _mockProductRepo.Setup(r => r.GetByIdAsync(102)).ReturnsAsync(product2);

            // Act
            var result = await _orderService.PlaceOrderAsync(order);

            // Assert
            Assert.True(result);
            Assert.Equal(3, product1.Stock); // 5 - 2 = 3
            Assert.Equal(5, product2.Stock); // 10 - 5 = 5

            _mockOrderRepo.Verify(r => r.BeginTransactionAsync(), Times.Once);
            _mockOrderRepo.Verify(r => r.AddAsync(order), Times.Once);
            _mockOrderRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockProductRepo.Verify(r => r.Update(product1), Times.Once);
            _mockProductRepo.Verify(r => r.Update(product2), Times.Once);
            _mockProductRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockOrderRepo.Verify(r => r.CommitTransactionAsync(), Times.Once);
            _mockOrderRepo.Verify(r => r.RollbackTransactionAsync(), Times.Never);
            
            _mockNotificationService.Verify(n => n.CreateNotificationAsync(
                10,
                NotificationType.Order,
                "Đặt hàng thành công! 🎉",
                It.Is<string>(s => s.Contains("Đơn hàng #1")),
                "/Account/Orders"
            ), Times.Once);
        }

        [Fact]
        public async Task PlaceOrderAsync_RollbacksAndReturnsFalse_WhenStockIsInsufficient()
        {
            // Arrange
            var product1 = new Product { Id = 101, Name = "Laptop", Stock = 1 }; // Only 1 in stock

            var order = new Order
            {
                Id = 1,
                UserId = 10,
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { ProductId = 101, Quantity = 3 } // Request 3
                }
            };

            _mockProductRepo.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(product1);

            // Act
            var result = await _orderService.PlaceOrderAsync(order);

            // Assert
            Assert.False(result);
            Assert.Equal(1, product1.Stock); // Stock unchanged

            _mockOrderRepo.Verify(r => r.BeginTransactionAsync(), Times.Once);
            _mockOrderRepo.Verify(r => r.CommitTransactionAsync(), Times.Never);
            _mockOrderRepo.Verify(r => r.RollbackTransactionAsync(), Times.Once);
            _mockNotificationService.Verify(n => n.CreateNotificationAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CancelOrderAsync_ReturnsFalse_WhenOrderDoesNotExist()
        {
            // Arrange
            int orderId = 1;
            int userId = 10;
            _mockOrderRepo.Setup(r => r.GetOrderWithDetailsAsync(orderId)).ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.CancelOrderAsync(orderId, userId);

            // Assert
            Assert.False(result);
            _mockOrderRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelOrderAsync_ReturnsFalse_WhenOrderDoesNotBelongToUser()
        {
            // Arrange
            int orderId = 1;
            int userId = 10;
            var order = new Order { Id = orderId, UserId = 99, Status = OrderStatus.Pending }; // Different user
            _mockOrderRepo.Setup(r => r.GetOrderWithDetailsAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CancelOrderAsync(orderId, userId);

            // Assert
            Assert.False(result);
            _mockOrderRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelOrderAsync_ReturnsFalse_WhenOrderStatusIsNotPending()
        {
            // Arrange
            int orderId = 1;
            int userId = 10;
            var order = new Order { Id = orderId, UserId = userId, Status = OrderStatus.Processing }; // Not pending
            _mockOrderRepo.Setup(r => r.GetOrderWithDetailsAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CancelOrderAsync(orderId, userId);

            // Assert
            Assert.False(result);
            _mockOrderRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelOrderAsync_ReturnsTrueAndRestoresStock_WhenOrderIsValidAndPending()
        {
            // Arrange
            int orderId = 1;
            int userId = 10;
            var product1 = new Product { Id = 101, Name = "Laptop", Stock = 5 };

            var order = new Order
            {
                Id = orderId,
                UserId = userId,
                Status = OrderStatus.Pending,
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { ProductId = 101, Quantity = 2 }
                }
            };

            _mockOrderRepo.Setup(r => r.GetOrderWithDetailsAsync(orderId)).ReturnsAsync(order);
            _mockProductRepo.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(product1);

            // Act
            var result = await _orderService.CancelOrderAsync(orderId, userId);

            // Assert
            Assert.True(result);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.NotNull(order.CancelledDate);
            Assert.Equal(7, product1.Stock); // 5 + 2 = 7 restored

            _mockOrderRepo.Verify(r => r.Update(order), Times.Once);
            _mockProductRepo.Verify(r => r.Update(product1), Times.Once);
            _mockOrderRepo.Verify(r => r.SaveChangesAsync(), Times.Once);

            _mockNotificationService.Verify(n => n.CreateNotificationAsync(
                userId,
                NotificationType.Order,
                "Đã hủy đơn hàng",
                It.Is<string>(s => s.Contains("Đơn hàng #1")),
                "/Account/Orders"
            ), Times.Once);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_UpdatesStatusToCompletedAndAwardsPoints_WhenStatusChangesToCompleted()
        {
            // Arrange
            int orderId = 1;
            var product1 = new Product { Id = 101, Name = "Laptop", SoldCount = 10 };
            
            var order = new Order
            {
                Id = orderId,
                UserId = 10,
                Status = OrderStatus.Processing,
                FinalAmount = 250000, // 250,000 VND -> Should earn 2 points
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { ProductId = 101, Quantity = 2 }
                }
            };

            _mockOrderRepo.Setup(r => r.GetOrderWithDetailsAsync(orderId)).ReturnsAsync(order);
            _mockProductRepo.Setup(r => r.GetByIdAsync(101)).ReturnsAsync(product1);

            // Act: Update to Completed
            await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Completed);

            // Assert
            Assert.Equal(OrderStatus.Completed, order.Status);
            Assert.NotNull(order.CompletedDate);
            Assert.Equal(12, product1.SoldCount); // 10 + 2 = 12 sold count

            _mockOrderRepo.Verify(r => r.Update(order), Times.Once);
            _mockOrderRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            
            // Check points earned
            _mockUserService.Verify(u => u.UpdateUserPointsAsync(10, 2), Times.Once);

            // Check notification
            _mockNotificationService.Verify(n => n.CreateNotificationAsync(
                10,
                NotificationType.Order,
                "Giao hàng thành công 📦",
                It.Is<string>(s => s.Contains("cộng thêm 2 điểm")),
                "/Account/Orders"
            ), Times.Once);

            // Check SignalR
            _mockHubClients.Verify(c => c.User("10"), Times.Once);
            _mockClientProxy.Verify(c => c.SendCoreAsync(
                "OrderStatusUpdated",
                It.Is<object?[]>(args => 
                    args != null && 
                    args.Length == 1 &&
                    args[0] != null &&
                    args[0]!.GetType().GetProperty("orderId") != null &&
                    (int)args[0]!.GetType().GetProperty("orderId")!.GetValue(args[0])! == orderId &&
                    args[0]!.GetType().GetProperty("status") != null &&
                    args[0]!.GetType().GetProperty("status")!.GetValue(args[0])!.ToString() == "Completed"
                ),
                default
            ), Times.Once);
        }
    }
}
