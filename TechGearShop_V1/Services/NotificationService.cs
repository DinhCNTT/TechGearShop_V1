using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.Enums;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using TechGearShop_V1.Hubs;

namespace TechGearShop_V1.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(INotificationRepository notificationRepository, IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(int userId, NotificationType type, string title, string message, string? linkTo = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                LinkTo = linkTo,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            // 🚀 Bắn Realtime sự kiện
            // Ở SignalR, ta gửi duy nhất riêng biệt cho UserId đó bằng cơ chế .User(userId.ToString())
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                typeString = notification.Type.ToString(),
                createdAt = notification.CreatedAt.ToString("HH:mm - dd/MM/yyyy"),
                linkTo = notification.LinkTo
            });
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int limit = 20)
        {
            return await _notificationRepository.GetUserNotificationsAsync(userId, limit);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null && notification.UserId == userId)
            {
                notification.IsRead = true;
                _notificationRepository.Update(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }
        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            return await _notificationRepository.DeleteNotificationAsync(notificationId, userId);
        }

        public async Task DeleteAllNotificationsAsync(int userId)
        {
            await _notificationRepository.DeleteAllNotificationsAsync(userId);
        }
    }
}
