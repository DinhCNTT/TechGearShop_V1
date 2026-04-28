using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TechGearShop_V1.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Khi user kết nối vào Socket, chúng ta sẽ quản lý Client Id
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
