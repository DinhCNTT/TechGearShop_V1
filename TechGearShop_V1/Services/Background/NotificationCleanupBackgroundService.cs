using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;

namespace TechGearShop_V1.Services.Background
{
    public class NotificationCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<NotificationCleanupBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        
        // Cứ mỗi 24 giờ sẽ chạy dọn dẹp 1 lần
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
        
        // Thời gian giữ lại thông báo là 90 ngày (khoảng 3 tháng)
        private const int RETENTION_DAYS = 90;

        public NotificationCleanupBackgroundService(
            ILogger<NotificationCleanupBackgroundService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Cleanup Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldNotificationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up old notifications.");
                }

                // Chờ 24 tiếng rồi mới chạy vòng lặp tiếp theo
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Notification Cleanup Background Service is stopping.");
        }

        private async Task CleanupOldNotificationsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting notification cleanup process at {time}", DateTimeOffset.Now);

            // Vì BackgroundService là Singleton (sống mãi), còn AppDbContext là Scoped (sống theo request),
            // nên ta phải tạo ra một Scope tạm thời để xin quyền truy cập vào Database.
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Tính toán mốc thời gian: Những thông báo tạo trước mốc này sẽ bị xóa
                var cutoffDate = DateTime.UtcNow.AddDays(-RETENTION_DAYS);

                // Sử dụng ExecuteDeleteAsync() (tính năng mới của EF Core 7+)
                // Lệnh này cực kỳ tối ưu vì nó dịch thẳng ra SQL "DELETE FROM Notifications WHERE..." 
                // và chạy trực tiếp dưới DB, KHÔNG tải hàng triệu dòng lên RAM máy chủ.
                int deletedCount = await dbContext.Notifications
                    .Where(n => n.CreatedAt < cutoffDate)
                    .ExecuteDeleteAsync(stoppingToken);

                _logger.LogInformation("Notification cleanup completed. Deleted {count} notifications older than {days} days.", deletedCount, RETENTION_DAYS);
            }
        }
    }
}
