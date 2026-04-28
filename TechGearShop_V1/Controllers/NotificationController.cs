using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        [HttpGet("GetList")]
        public async Task<IActionResult> GetList(int limit = 20)
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, limit);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            
            return Ok(new {
                Success = true,
                Data = notifications.Select(n => new {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    TypeString = n.Type.ToString(),
                    n.IsRead,
                    n.LinkTo,
                    CreatedAt = n.CreatedAt.ToString("HH:mm - dd/MM/yyyy")
                }),
                UnreadCount = unreadCount
            });
        }

        [HttpPost("MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id, GetUserId());
            return Ok(new { Success = true });
        }

        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(GetUserId());
            return Ok(new { Success = true });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _notificationService.DeleteNotificationAsync(id, GetUserId());
            return success ? Ok(new { Success = true }) : NotFound(new { Success = false });
        }

        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAll()
        {
            await _notificationService.DeleteAllNotificationsAsync(GetUserId());
            return Ok(new { Success = true });
        }
    }
}
