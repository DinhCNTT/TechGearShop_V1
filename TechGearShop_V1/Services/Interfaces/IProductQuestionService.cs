using System.Collections.Generic;
using System.Threading.Tasks;
using TechGearShop_V1.Models.DTOs;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IProductQuestionService
    {
        /// <summary>
        /// Lấy lịch sử câu hỏi gần nhất của sản phẩm (phân trang).
        /// </summary>
        Task<IEnumerable<QuestionDto>> GetHistoryAsync(int productId, int take, int skip = 0);

        /// <summary>
        /// Đếm tổng số câu hỏi của sản phẩm.
        /// </summary>
        Task<int> GetTotalCountAsync(int productId);

        /// <summary>
        /// Gửi câu hỏi hoặc trả lời mới.
        /// Trả về (Success, Message, Data) — Data là DTO đã được render sẵn để broadcast.
        /// </summary>
        Task<(bool Success, string Message, QuestionDto? Data)> PostQuestionAsync(
            int userId, int productId, string content, bool isAdminReply, int? parentId = null);
    }
}
