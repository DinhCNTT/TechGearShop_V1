using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Repositories.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int limit = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);
        Task DeleteAllNotificationsAsync(int userId);
    }
}
