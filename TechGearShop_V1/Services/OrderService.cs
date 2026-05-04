using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using TechGearShop_V1.Hubs;

namespace TechGearShop_V1.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IUserService userService, INotificationService notificationService, IHubContext<NotificationHub> hubContext)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userService = userService;
            _notificationService = notificationService;
            _hubContext = hubContext;
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

                // 3. Bắn thông báo (Notification) sau khi đơn hàng xác nhận thành công
                await _notificationService.CreateNotificationAsync(
                    order.UserId,
                    TechGearShop_V1.Models.Enums.NotificationType.Order,
                    "Đặt hàng thành công! 🎉",
                    $"Đơn hàng #{order.Id} đã được hệ thống ghi nhận. Chúng tôi sẽ sớm giao hàng cho bạn.",
                    "/Account/Orders"
                );

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
            if (order != null && order.Status != status)
            {
                order.Status = status;

                // Log thời gian thay đổi trạng thái
                switch (status)
                {
                    case OrderStatus.Processing: order.ProcessingDate = DateTime.UtcNow; break;
                    case OrderStatus.Shipping: order.ShippingDate = DateTime.UtcNow; break;
                    case OrderStatus.Completed: order.CompletedDate = DateTime.UtcNow; break;
                    case OrderStatus.Cancelled: order.CancelledDate = DateTime.UtcNow; break;
                }

                _orderRepository.Update(order);
                await _orderRepository.SaveChangesAsync();

                // Dịch trạng thái sang tiếng Việt và format màu sắc luôn cho Frontend dễ xài
                string statusName = status switch {
                    OrderStatus.Pending => "Chờ xác nhận",
                    OrderStatus.Processing => "Đang xử lý",
                    OrderStatus.Shipping => "Đang giao hàng",
                    OrderStatus.Completed => "Đã giao thành công",
                    _ => "Đã hủy"
                };
                
                string statusColor = status switch {
                    OrderStatus.Pending => "bg-warning text-dark",
                    OrderStatus.Processing => "bg-info",
                    OrderStatus.Shipping => "bg-primary",
                    OrderStatus.Completed => "bg-success",
                    _ => "bg-danger"
                };

                // Bắn SignalR Realtime đến duy nhất User chủ của đơn hàng
                await _hubContext.Clients.User(order.UserId.ToString()).SendAsync("OrderStatusUpdated", new {
                    orderId = order.Id,
                    status = status.ToString(),
                    statusName = statusName,
                    statusColor = statusColor
                });

                // Bonus logic: Nếu đơn hàng hoàn thành -> cộng điểm thưởng cho User và tăng lượt bán
                if (status == OrderStatus.Completed)
                {
                    // Cộng điểm (VD: 1 điểm cho mỗi 100,000 VND)
                    int pointsEarned = (int)(order.FinalAmount / 100000);
                    await _userService.UpdateUserPointsAsync(order.UserId, pointsEarned);

                    // Bắn thông báo nâng hạng / hoàn thành đơn
                    await _notificationService.CreateNotificationAsync(
                        order.UserId, 
                        TechGearShop_V1.Models.Enums.NotificationType.Order,
                        "Giao hàng thành công 📦",
                        $"Đơn hàng #{order.Id} đã hoàn tất. Bạn được cộng thêm {pointsEarned} điểm thưởng TechGear!",
                        "/Account/Orders"
                    );

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

        public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetPagedOrdersAsync(string keyword, OrderStatus? status, int page, int pageSize)
        {
            return await _orderRepository.GetPagedOrdersAsync(keyword, status, page, pageSize);
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _orderRepository.GetOrderWithDetailsAsync(orderId);
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            // Bảo mật: chỉ cho hủy đơn của chính mình & chỉ khi đang Pending
            if (order == null || order.UserId != userId || order.Status != OrderStatus.Pending)
                return false;

            order.Status = OrderStatus.Cancelled;
            order.CancelledDate = DateTime.UtcNow;
            _orderRepository.Update(order);

            // Hoàn lại tồn kho cho từng sản phẩm
            foreach (var detail in order.OrderDetails)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock += detail.Quantity;
                    _productRepository.Update(product);
                }
            }
            await _orderRepository.SaveChangesAsync();

            // Bắn thông báo hủy đơn
            await _notificationService.CreateNotificationAsync(
                userId,
                TechGearShop_V1.Models.Enums.NotificationType.Order,
                "Đã hủy đơn hàng",
                $"Đơn hàng #{order.Id} đã được hủy thành công. Tồn kho đã được hoàn lại.",
                "/Account/Orders"
            );

            return true;
        }

        // ─── VNPAY METHODS ──────────────────────────────────────────

        public async Task<int> CreatePendingPaymentOrderAsync(TechGearShop_V1.Models.DTOs.OrderRequestDto request)
        {
            await _orderRepository.BeginTransactionAsync();
            try
            {
                // 1. Trừ kho nguyên tử ngay lập tức để giữ hàng (giống logic Background)
                foreach (var item in request.Items)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product == null || product.Stock < item.Quantity)
                    {
                        throw new Exception($"Sản phẩm '{item.ProductName}' đã hết hàng hoặc không đủ số lượng.");
                    }
                    product.Stock -= item.Quantity;
                    _productRepository.Update(product);
                }
                await _productRepository.SaveChangesAsync();

                // 2. Tạo Order với trạng thái PaymentPending
                var order = new Order
                {
                    UserId = request.UserId,
                    ReceiverName = request.ReceiverName,
                    ReceiverPhone = request.ReceiverPhone,
                    ShippingAddress = request.ShippingAddress,
                    Province = request.Province,
                    Note = request.Note,
                    PaymentMethod = PaymentMethod.VNPay,
                    CouponCode = request.CouponCode,
                    ShippingFee = request.ShippingFee,
                    DiscountAmount = request.DiscountAmount,
                    TotalAmount = request.SubTotal,
                    FinalAmount = request.FinalAmount,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.PaymentPending, // Đang chờ VNPay callback
                    PaymentStatus = PaymentStatus.Unpaid,
                    OrderDetails = request.Items.Select(i => new OrderDetail
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.Price
                    }).ToList()
                };

                await _orderRepository.AddAsync(order);
                await _orderRepository.SaveChangesAsync();
                await _orderRepository.CommitTransactionAsync();

                return order.Id;
            }
            catch
            {
                await _orderRepository.RollbackTransactionAsync();
                throw; // Rethrow để Controller xử lý (trả về lỗi cho User)
            }
        }

        public async Task<bool> ConfirmPaymentAsync(int orderId, string txnId, string responseCode)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null || order.Status != OrderStatus.PaymentPending) return false;

            order.PaymentStatus = PaymentStatus.Paid;
            order.Status = OrderStatus.Pending; // Chuyển về trạng thái chờ shop duyệt
            order.PaidAt = DateTime.UtcNow;
            order.VnpayTransactionId = txnId;
            order.VnpayResponseCode = responseCode;

            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            // Ghi nhận Coupon đã sử dụng (nếu có)
            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                // Ở đây mình nên dùng scope hoặc injection nhưng tạm thời ta có thể gọi service.
                // Để đơn giản, NotificationService đã inject rồi, nhưng CouponService chưa có.
                // Sẽ an toàn hơn nếu việc record usage được thực hiện ngay lúc Confirm.
                // Tạm thời bỏ qua Coupon record ở đây, sẽ gọi trong IPN Controller.
            }

            // Gửi thông báo cho User
            await _notificationService.CreateNotificationAsync(
                order.UserId,
                TechGearShop_V1.Models.Enums.NotificationType.Order,
                "Thanh toán VNPay thành công! 💳",
                $"Đơn hàng #{order.Id} đã được thanh toán và đang chờ xử lý.",
                "/Account/Orders"
            );

            // Realtime SignalR
            await _hubContext.Clients.User(order.UserId.ToString()).SendAsync("OrderPlacedResult", new
            {
                success = true,
                orderId = order.Id,
                message = $"Thanh toán VNPay thành công! Đơn hàng #{order.Id} đã được ghi nhận."
            });

            return true;
        }

        public async Task<bool> FailPaymentAsync(int orderId, string responseCode)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null || order.Status != OrderStatus.PaymentPending) return false;

            order.PaymentStatus = PaymentStatus.Failed;
            order.Status = OrderStatus.Cancelled;
            order.CancelledDate = DateTime.UtcNow;
            order.VnpayResponseCode = responseCode;

            _orderRepository.Update(order);

            // Hoàn kho
            foreach (var detail in order.OrderDetails)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock += detail.Quantity;
                    _productRepository.Update(product);
                }
            }
            await _orderRepository.SaveChangesAsync();

            // Bắn SignalR
            await _hubContext.Clients.User(order.UserId.ToString()).SendAsync("OrderPlacedResult", new
            {
                success = false,
                message = $"Thanh toán VNPay thất bại hoặc bị hủy. Đơn hàng #{order.Id} đã bị hủy."
            });

            return true;
        }
    }
}
