using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<bool> RegisterAsync(User user, string password);
        Task<User?> GetUserByIdAsync(int id);
        Task UpdateUserPointsAsync(int userId, int pointsToAdd);
    }
}
