using System.Threading.Tasks;
using TechGearShop_V1.Models.DTOs;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IReviewService
    {
        Task<PaginatedReviewDto> GetReviewsPaginatedAsync(int productId, int page, int pageSize);
        Task<bool> CanUserReviewAsync(int userId, int productId);
        Task<(bool Success, string Message)> AddReviewAsync(int userId, int productId, int rating, string content);
        Task<ReviewDto?> GetMyReviewAsync(int userId, int productId);
    }
}
