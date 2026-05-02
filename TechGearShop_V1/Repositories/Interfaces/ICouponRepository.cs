using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Repositories.Interfaces
{
    public interface ICouponRepository : IGenericRepository<Coupon>
    {
        Task<Coupon?> GetByCodeAsync(string code);
        Task<IEnumerable<Coupon>> GetAllWithUsageAsync();
        Task<bool> HasUserUsedCouponAsync(int couponId, int userId);
        Task AddCouponUsageAsync(CouponUsage usage);
        Task IncrementUsageCountAsync(int couponId);
    }
}
