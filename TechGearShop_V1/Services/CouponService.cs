using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepository;

        public CouponService(ICouponRepository couponRepository)
        {
            _couponRepository = couponRepository;
        }

        public async Task<Coupon?> GetValidCouponAsync(string code, decimal currentOrderValue)
        {
            var coupon = await _couponRepository.GetByCodeAsync(code);
            
            if (coupon == null || !coupon.IsActive) return null;
            if (coupon.ExpiryDate < DateTime.UtcNow) return null;
            if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value) return null;
            if (currentOrderValue < coupon.MinOrderValue) return null;

            return coupon;
        }

        public async Task ApplyCouponUsageAsync(int couponId)
        {
            var coupon = await _couponRepository.GetByIdAsync(couponId);
            if (coupon != null)
            {
                coupon.UsageCount++;
                _couponRepository.Update(coupon);
                await _couponRepository.SaveChangesAsync();
            }
        }
    }
}
