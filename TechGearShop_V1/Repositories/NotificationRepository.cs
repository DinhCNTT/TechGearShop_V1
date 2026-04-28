using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;

namespace TechGearShop_V1.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int limit = 20)
        {
            return await _dbSet
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notif in unreadNotifications)
                {
                    notif.IsRead = true;
                }
                _dbSet.UpdateRange(unreadNotifications);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            var notif = await _dbSet.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null) return false;
            _dbSet.Remove(notif);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAllNotificationsAsync(int userId)
        {
            var notifs = await _dbSet.Where(n => n.UserId == userId).ToListAsync();
            if (notifs.Any())
            {
                _dbSet.RemoveRange(notifs);
                await _context.SaveChangesAsync();
            }
        }
    }
}
