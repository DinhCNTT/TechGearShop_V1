using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.Enums;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services.Background
{
    public class RestockNotificationBackgroundService : BackgroundService
    {
        private readonly IStockNotificationQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RestockNotificationBackgroundService> _logger;

        public RestockNotificationBackgroundService(
            IStockNotificationQueue queue,
            IServiceProvider serviceProvider,
            ILogger<RestockNotificationBackgroundService> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Restock Notification Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var productId = await _queue.DequeueAsync(stoppingToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();
                        await ProcessRestockAsync(productId, dbContext, emailService, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Catch gracefully
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing restock notification job.");
                }
            }
        }

        private async Task ProcessRestockAsync(int productId, AppDbContext dbContext, IEmailSenderService emailService, CancellationToken stoppingToken)
        {
            var product = await dbContext.Products.FindAsync(new object[] { productId }, stoppingToken);
            if (product == null || product.Stock <= 0) return;

            var subscriptions = await dbContext.StockSubscriptions
                .Include(s => s.User)
                .Where(s => s.ProductId == productId && s.Status == StockSubscriptionStatus.Pending)
                .ToListAsync(stoppingToken);

            if (!subscriptions.Any()) return;

            _logger.LogInformation($"Found {subscriptions.Count} waiting users for product '{product.Name}'. Dispatching Real HTML Emails...");

            foreach (var sub in subscriptions)
            {
                string targetEmail = sub.UserId.HasValue ? sub.User.Email : sub.GuestEmail;
                
                string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<style>
  body {{ font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f4f7f6; padding: 0; margin: 0; }}
  .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.05); }}
  .header {{ background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); padding: 35px 20px; text-align: center; color: #ffffff; }}
  .header h1 {{ margin: 0; font-size: 28px; font-weight: 800; letter-spacing: 1.5px; text-shadow: 0 2px 4px rgba(0,0,0,0.2); }}
  .content {{ padding: 45px 40px; color: #444; line-height: 1.7; font-size: 15px; }}
  .content h2 {{ color: #1e3c72; font-size: 22px; margin-top: 0; margin-bottom: 25px; font-weight: 700; border-bottom: 2px solid #f0f4f8; padding-bottom: 15px; }}
  .product-box {{ background: linear-gradient(to right, #f8f9fa, #ffffff); border-left: 4px solid #f59e0b; border-radius: 8px; padding: 25px; margin: 30px 0; box-shadow: 0 2px 10px rgba(0,0,0,0.02); }}
  .product-name {{ font-size: 19px; font-weight: 700; color: #1e3c72; margin: 0 0 12px 0; }}
  .product-price {{ font-size: 16px; color: #4b5563; margin: 0; font-weight: 500; }}
  .highlight {{ color: #e11d48; font-weight: 700; font-size: 18px; }}
  .btn-wrapper {{ text-align: center; margin-top: 40px; margin-bottom: 20px; }}
  .btn {{ display: inline-block; background: linear-gradient(135deg, #2563eb, #1d4ed8); color: #ffffff !important; padding: 15px 35px; text-decoration: none; border-radius: 30px; font-weight: 700; font-size: 16px; box-shadow: 0 4px 15px rgba(37, 99, 235, 0.3); letter-spacing: 0.5px; }}
  .footer {{ background-color: #f8fafc; padding: 30px 40px; color: #64748b; font-size: 13px; text-align: center; border-top: 1px solid #e2e8f0; line-height: 1.6; }}
</style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>⚙️ TechGear Shop</h1>
    </div>
    <div class='content'>
      <h2>🎉 Tin Vui: Sản Phẩm Đã Có Hàng!</h2>
      <p>Kính chào Quý khách,</p>
      <p>TechGear Shop vô cùng vui mừng thông báo: Sản phẩm mà Quý khách hằng mong đợi hiện đã <strong>có sẵn hàng</strong> tại hệ thống của chúng tôi! 🚀</p>
      
      <div class='product-box'>
         <p class='product-name'>📦 {product.Name}</p>
         <p class='product-price'>💰 Giá hiện tại: <span class='highlight'>{product.Price:N0} ₫</span></p>
         <p class='product-price' style='margin-top: 8px;'>🔥 Số lượng nhập kho: <strong>{product.Stock}</strong> sản phẩm</p>
      </div>

      <p>Do số lượng nhập về cực kỳ có hạn và nhu cầu rất cao, Quý khách hãy nhanh tay truy cập website để sở hữu ngay siêu phẩm này trước khi lại ""cháy hàng"" nhé! ⏰</p>
      
      <div class='btn-wrapper'>
        <a href='http://localhost:5066/Product/Detail/{product.Id}' class='btn'>👉 ĐẶT HÀNG NGAY 👈</a>
      </div>
    </div>
    <div class='footer'>
      ❤️ Trân trọng cảm ơn Quý khách đã tin tưởng và đồng hành cùng TechGear Shop.<br/><br/>
      © {DateTime.Now.Year} TechGear Shop. All rights reserved.<br/>
      <i>Đây là email tự động, vui lòng không phản hồi lại email này.</i>
    </div>
  </div>
</body>
</html>";

                await emailService.SendEmailAsync(targetEmail, $"[TechGear] Tin vui: {product.Name} đã có hàng!", htmlContent);
                _logger.LogInformation($"[REAL EMAIL DISPATCHED] To: {targetEmail}");
                
                sub.Status = StockSubscriptionStatus.Notified;
                sub.NotifiedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
