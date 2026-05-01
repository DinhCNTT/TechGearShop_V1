using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Hubs
{
    /// <summary>
    /// SignalR Hub cho tính năng Hỏi đáp nhanh trên trang sản phẩm.
    /// Không yêu cầu [Authorize] — ai cũng xem được, nhưng phải đăng nhập mới gửi được.
    /// </summary>
    public class QaHub : Hub
    {
        private readonly IProductQuestionService _qaService;

        public QaHub(IProductQuestionService qaService)
        {
            _qaService = qaService;
        }

        /// <summary>
        /// Client gọi khi vào trang sản phẩm → tham gia "phòng" riêng của sản phẩm đó.
        /// Đảm bảo chỉ broadcast trong cùng sản phẩm, không fan-out toàn server.
        /// </summary>
        public async Task JoinProductRoom(string productId)
        {
            if (int.TryParse(productId, out _))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"product_{productId}");
            }
        }

        /// <summary>
        /// Client gọi khi rời trang sản phẩm.
        /// </summary>
        public async Task LeaveProductRoom(string productId)
        {
            if (int.TryParse(productId, out _))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"product_{productId}");
            }
        }

        /// <summary>
        /// Client gửi câu hỏi mới. Phải đăng nhập.
        /// </summary>
        public async Task SendQuestion(int productId, string content, int? parentId = null)
        {
            // Kiểm tra đăng nhập
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                await Clients.Caller.SendAsync("QaError", "Vui lòng đăng nhập để đặt câu hỏi.");
                return;
            }

            // Xác định Admin hay Customer
            bool isAdmin = Context.User?.IsInRole(UserRole.Admin.ToString()) ?? false;

            // Gọi Service xử lý (validate, anti-spam, sanitize, lưu DB)
            var result = await _qaService.PostQuestionAsync(userId, productId, content, isAdmin, parentId);

            if (!result.Success)
            {
                // Gửi lỗi chỉ cho người gửi (Caller)
                await Clients.Caller.SendAsync("QaError", result.Message);
                return;
            }

            // Broadcast câu hỏi mới đến TẤT CẢ người trong "phòng" sản phẩm
            await Clients.Group($"product_{productId}").SendAsync("ReceiveQuestion", result.Data);

            // Gửi xác nhận thành công cho người gửi
            await Clients.Caller.SendAsync("QaSuccess", result.Message);
        }
    }
}
