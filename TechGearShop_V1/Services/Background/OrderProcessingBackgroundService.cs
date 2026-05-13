using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Hubs;
using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.Enums;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services.Background
{
    /// <summary>
    /// Background Worker — liên tục đọc đơn hàng từ IOrderChannel và xử lý tuần tự.
    ///
    /// Tại sao xử lý tuần tự (1-by-1)?
    ///   - Đảm bảo tính ATOMIC: Chỉ 1 goroutine chạm vào cột Stock trong một thời điểm.
    ///   - Tránh hoàn toàn Race Condition ở tầng ứng dụng.
    ///   - SQL ExecuteUpdateAsync thêm một lớp khóa nữa ở tầng DB để chắc chắn tuyệt đối.
    /// </summary>
    public sealed class OrderProcessingBackgroundService : BackgroundService
    {
        // Dùng IServiceScopeFactory vì BackgroundService là Singleton
        // nhưng DbContext và các Scoped Service phải được resolve trong Scope riêng.
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOrderChannel        _orderChannel;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<OrderProcessingBackgroundService> _logger;

        public OrderProcessingBackgroundService(
            IServiceScopeFactory             scopeFactory,
            IOrderChannel                    orderChannel,
            IHubContext<NotificationHub>     hubContext,
            ILogger<OrderProcessingBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _orderChannel = orderChannel;
            _hubContext   = hubContext;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[OrderProcessor] 🚀 Background worker đã khởi động.");

            // ReadAllAsync sẽ chạy mãi cho đến khi CancellationToken bị kích hoạt (app shutdown)
            await foreach (var request in _orderChannel.ReadAllAsync(stoppingToken))
            {
                await ProcessOrderAsync(request, stoppingToken);
            }

            _logger.LogInformation("[OrderProcessor] 🛑 Background worker đã dừng.");
        }

        private async Task ProcessOrderAsync(OrderRequestDto request, CancellationToken ct)
        {
            // Tạo scope mới để dùng các Scoped service (DbContext, NotificationService...)
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db                  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // ── Bước 1: Trừ kho nguyên tử (ATOMIC) cho từng sản phẩm ──────────────
                // ExecuteUpdateAsync sinh ra câu lệnh SQL:
                //   UPDATE Products SET Stock = Stock - {qty}
                //   WHERE Id = {id} AND Stock >= {qty}
                // SQL Server sẽ tự Lock dòng đó trong tích tắc. Race Condition hoàn toàn bị chặn.
                foreach (var item in request.Items)
                {
                    int rowsAffected = await db.Products
                        .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(p => p.Stock, p => p.Stock - item.Quantity),
                            ct);

                    // Nếu rowsAffected = 0 → sản phẩm đã hết hàng hoặc không đủ số lượng
                    if (rowsAffected == 0)
                    {
                        await transaction.RollbackAsync(ct);

                        _logger.LogWarning(
                            "[OrderProcessor] ❌ Hết hàng — ProductId={ProductId}, UserId={UserId}",
                            item.ProductId, request.UserId);

                        // Bắn SignalR báo thất bại
                        await _hubContext.Clients
                            .User(request.UserId.ToString())
                            .SendAsync("OrderPlacedResult", new
                            {
                                success = false,
                                message = $"Rất tiếc! Sản phẩm '{item.ProductName}' đã hết hàng hoặc không đủ số lượng."
                            }, ct);

                        return;
                    }
                }

                // ── Bước 1.5: Lấy CostPrice của các sản phẩm để lưu vào OrderDetail ───────────
                var productIds = request.Items.Select(i => i.ProductId).ToList();
                var costPrices = await db.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.CostPrice, ct);

                // ── Bước 2: Lưu Order + OrderDetail vào DB ───────────────────────────────
                var order = new Order
                {
                    UserId          = request.UserId,
                    ReceiverName    = request.ReceiverName,
                    ReceiverPhone   = request.ReceiverPhone,
                    ShippingAddress = request.ShippingAddress,
                    Province        = request.Province,
                    PaymentMethod   = request.PaymentMethod,
                    CouponCode      = request.CouponCode,
                    Note            = request.Note,
                    ShippingFee     = request.ShippingFee,
                    DiscountAmount  = request.DiscountAmount,
                    TotalAmount     = request.SubTotal,
                    FinalAmount     = request.FinalAmount,
                    OrderDate       = DateTime.UtcNow,
                    Status          = OrderStatus.Pending,
                    OrderDetails    = request.Items.Select(i => new OrderDetail
                    {
                        ProductId     = i.ProductId,
                        Quantity      = i.Quantity,
                        UnitPrice     = i.Price,
                        UnitCostPrice = costPrices.GetValueOrDefault(i.ProductId, 0m)
                    }).ToList()
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                // ── Bước 2b: Ghi nhận coupon usage (sau khi đơn hàng đã commit) ─────
                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var couponService = scope.ServiceProvider.GetRequiredService<ICouponService>();
                    await couponService.RecordUsageAsync(request.CouponCode, request.UserId, order.Id);
                }

                _logger.LogInformation(
                    "[OrderProcessor] ✅ Đơn hàng #{OrderId} đã được tạo — UserId={UserId}",
                    order.Id, request.UserId);

                // ── Bước 3: Tạo Notification + Bắn SignalR "Thành công" ─────────────────
                await notificationService.CreateNotificationAsync(
                    request.UserId,
                    NotificationType.Order,
                    "Đặt hàng thành công! 🎉",
                    $"Đơn hàng #{order.Id} đã được ghi nhận. Chúng tôi sẽ sớm giao hàng cho bạn.",
                    "/Account/Orders");

                await _hubContext.Clients
                    .User(request.UserId.ToString())
                    .SendAsync("OrderPlacedResult", new
                    {
                        success = true,
                        orderId = order.Id,
                        message = $"🎉 Đặt hàng thành công! Đơn hàng #{order.Id} đã được ghi nhận."
                    }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[OrderProcessor] 💥 Lỗi khi xử lý đơn hàng — UserId={UserId}", request.UserId);

                try { await transaction.RollbackAsync(ct); } catch { /* Ignore rollback error */ }

                // Báo lỗi về cho User qua SignalR
                await _hubContext.Clients
                    .User(request.UserId.ToString())
                    .SendAsync("OrderPlacedResult", new
                    {
                        success = false,
                        message = "Hệ thống gặp sự cố khi xử lý đơn hàng của bạn. Vui lòng thử lại."
                    }, ct);
            }
        }
    }
}
