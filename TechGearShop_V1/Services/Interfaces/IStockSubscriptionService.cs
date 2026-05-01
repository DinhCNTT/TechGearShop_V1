using System.Threading.Tasks;
using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IStockSubscriptionService
    {
        Task<(bool Success, string Message)> SubscribeAsync(int productId, int? userId, string guestEmail);
    }
}
