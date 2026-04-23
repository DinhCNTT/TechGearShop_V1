using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || user.IsLocked) return null;

            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return user;
            }

            return null;
        }

        public async Task<bool> RegisterAsync(User user, string password)
        {
            // Kiểm tra username hoặc email tồn tại chưa
            var existingUser = await _userRepository.GetByUsernameAsync(user.Username);
            if (existingUser != null) return false;

            var existingEmail = await _userRepository.GetByEmailAsync(user.Email);
            if (existingEmail != null) return false;

            // Mã hóa mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task UpdateUserPointsAsync(int userId, int pointsToAdd)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.Points += pointsToAdd;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            }
        }
    }
}
