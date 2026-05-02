using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepository;
        private readonly ILogger<CouponService> _logger;

        public CouponService(ICouponRepository couponRepository, ILogger<CouponService> logger)
        {
            _couponRepository = couponRepository;
            _logger = logger;
        }

        // ── Validation Chain ──────────────────────────────────────────────────────

        public async Task<CouponValidationResult> ValidateCouponAsync(
            string code, decimal subTotal, decimal shippingFee, int? userId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Fail("Vui lòng nhập mã giảm giá.");

            var coupon = await _couponRepository.GetByCodeAsync(code.Trim().ToUpperInvariant());

            if (coupon == null)
                return Fail("Mã giảm giá không tồn tại.");

            if (!coupon.IsActive)
                return Fail("Mã giảm giá đã bị vô hiệu hóa.");

            if (coupon.IsNotStarted)
                return Fail($"Mã chưa có hiệu lực. Bắt đầu từ {coupon.StartDate:dd/MM/yyyy}.");

            if (coupon.IsExpired)
                return Fail("Mã giảm giá đã hết hạn.");

            if (coupon.IsExhausted)
                return Fail("Mã đã đạt giới hạn sử dụng tối đa.");

            if (userId.HasValue)
            {
                bool alreadyUsed = await _couponRepository.HasUserUsedCouponAsync(coupon.Id, userId.Value);
                if (alreadyUsed)
                    return Fail("Bạn đã sử dụng mã giảm giá này rồi.");
            }

            if (subTotal < coupon.MinOrderValue)
                return Fail($"Đơn hàng tối thiểu {coupon.MinOrderValue:N0}đ để áp dụng mã này.");

            // ── Tính discount theo loại ───────────────────────────────────────────
            decimal discountAmount = 0;
            decimal shippingDiscount = 0;

            switch (coupon.DiscountType)
            {
                case DiscountType.Percentage:
                    discountAmount = subTotal * coupon.DiscountValue / 100m;
                    if (coupon.MaxDiscountAmount.HasValue)
                        discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount.Value);
                    discountAmount = Math.Min(discountAmount, subTotal);
                    break;

                case DiscountType.FixedAmount:
                    discountAmount = Math.Min(coupon.DiscountValue, subTotal);
                    break;

                case DiscountType.FreeShipping:
                    shippingDiscount = shippingFee; // Giảm toàn bộ phí ship
                    break;
            }

            var msg = coupon.DiscountType == DiscountType.FreeShipping
                ? "Áp dụng thành công! Miễn phí vận chuyển 🚚"
                : $"Áp dụng thành công! Giảm {discountAmount:N0}đ 🎉";

            return new CouponValidationResult
            {
                IsValid        = true,
                Message        = msg,
                DiscountAmount = discountAmount,
                ShippingDiscount = shippingDiscount,
                CouponId       = coupon.Id
            };
        }

        /// <summary>
        /// Ghi lại usage SAU KHI đơn hàng được commit thành công.
        /// Dùng ExecuteUpdateAsync để atomic increment — tránh race condition.
        /// </summary>
        public async Task RecordUsageAsync(string code, int userId, int orderId)
        {
            try
            {
                var coupon = await _couponRepository.GetByCodeAsync(code);
                if (coupon == null) return;

                // Ghi usage record (Unique constraint DB bảo vệ duplicate)
                await _couponRepository.AddCouponUsageAsync(new CouponUsage
                {
                    CouponId = coupon.Id,
                    UserId   = userId,
                    OrderId  = orderId,
                    UsedAt   = DateTime.UtcNow
                });

                // Atomic increment count
                await _couponRepository.IncrementUsageCountAsync(coupon.Id);
                await _couponRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "[CouponService] Recorded usage — Code={Code}, UserId={UserId}, OrderId={OrderId}",
                    code, userId, orderId);
            }
            catch (Exception ex)
            {
                // Không chặn luồng chính — chỉ log, đơn hàng đã thành công rồi
                _logger.LogError(ex,
                    "[CouponService] Failed to record coupon usage — Code={Code}", code);
            }
        }

        // ── Admin CRUD ────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Coupon>> GetAllCouponsAsync()
            => await _couponRepository.GetAllWithUsageAsync();

        public async Task<Coupon?> GetByIdAsync(int id)
            => await _couponRepository.GetByIdAsync(id);

        public async Task<Coupon> CreateCouponAsync(CouponFormDto dto)
        {
            var coupon = MapFromDto(dto, new Coupon());
            coupon.Code = dto.Code.Trim().ToUpperInvariant(); // Normalize trước khi lưu
            await _couponRepository.AddAsync(coupon);
            await _couponRepository.SaveChangesAsync();
            return coupon;
        }

        public async Task UpdateCouponAsync(int id, CouponFormDto dto)
        {
            var coupon = await _couponRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Coupon #{id} không tồn tại.");

            coupon = MapFromDto(dto, coupon);
            coupon.Code = dto.Code.Trim().ToUpperInvariant();
            _couponRepository.Update(coupon);
            await _couponRepository.SaveChangesAsync();
        }

        public async Task DeleteCouponAsync(int id)
        {
            var coupon = await _couponRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Coupon #{id} không tồn tại.");

            _couponRepository.Delete(coupon);
            await _couponRepository.SaveChangesAsync();
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var coupon = await _couponRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Coupon #{id} không tồn tại.");

            coupon.IsActive = !coupon.IsActive;
            _couponRepository.Update(coupon);
            await _couponRepository.SaveChangesAsync();
            return coupon.IsActive;
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private static CouponValidationResult Fail(string message) => new()
        {
            IsValid = false,
            Message = message
        };

        private static Coupon MapFromDto(CouponFormDto dto, Coupon target)
        {
            target.Description      = dto.Description;
            target.DiscountType     = dto.DiscountType;
            target.DiscountValue    = dto.DiscountValue;
            target.MaxDiscountAmount = dto.MaxDiscountAmount;
            target.MinOrderValue    = dto.MinOrderValue;
            target.StartDate        = dto.StartDate.ToUniversalTime();
            target.ExpiryDate       = dto.ExpiryDate.ToUniversalTime();
            target.UsageLimit       = dto.UsageLimit;
            target.IsActive         = dto.IsActive;
            return target;
        }
    }
}
