using Microsoft.Extensions.Caching.Memory;
using TechGearShop_V1.Data;

namespace TechGearShop_V1.Services
{
    /// <summary>
    /// Background service: gom lượt xem từ IMemoryCache, ghi batch vào DB mỗi 5 phút.
    /// Pattern chuẩn large-scale: tránh N DB writes đồng thời làm chậm request.
    /// </summary>
    public class ViewCountFlushService : BackgroundService
    {
        private static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ViewCountFlushService> _logger;

        // Thread-safe dictionary: ProductId → pending view increment
        public static System.Collections.Concurrent.ConcurrentDictionary<int, int> PendingViews
            = new();

        public ViewCountFlushService(IServiceScopeFactory scopeFactory, IMemoryCache cache, ILogger<ViewCountFlushService> logger)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ViewCountFlushService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(FlushInterval, stoppingToken);
                await FlushAsync();
            }
        }

        private async Task FlushAsync()
        {
            if (PendingViews.IsEmpty) return;

            // Drain the dictionary atomically
            var snapshot = PendingViews.Keys.ToList();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            int flushed = 0;
            foreach (var productId in snapshot)
            {
                if (PendingViews.TryRemove(productId, out int increment) && increment > 0)
                {
                    var product = await db.Products.FindAsync(productId);
                    if (product != null)
                    {
                        product.ViewCount += increment;
                        flushed++;
                    }
                }
            }

            if (flushed > 0)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation("ViewCountFlushService: flushed {Count} products.", flushed);
            }
        }
    }
}
