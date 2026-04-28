using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.Enums;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int limit = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task CreateNotificationAsync(int userId, NotificationType type, string title, string message, string? linkTo = null);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);
        Task DeleteAllNotificationsAsync(int userId);
    }
}
