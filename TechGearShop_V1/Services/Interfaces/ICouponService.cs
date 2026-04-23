using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface ICouponService
    {
        Task<Coupon?> GetValidCouponAsync(string code, decimal currentOrderValue);
        Task ApplyCouponUsageAsync(int couponId);
    }
}
