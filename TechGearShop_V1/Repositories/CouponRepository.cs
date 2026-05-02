using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Repositories.Interfaces;

namespace TechGearShop_V1.Repositories
{
    public class CouponRepository : GenericRepository<Coupon>, ICouponRepository
    {
        public CouponRepository(AppDbContext context) : base(context) { }

        /// <summary>Case-insensitive lookup — normalize về UPPER để tránh DB collation issues.</summary>
        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            var normalized = code.Trim().ToUpperInvariant();
            return await _dbSet
                .Include(c => c.Usages)
                .FirstOrDefaultAsync(c => c.Code == normalized);
        }

        public async Task<IEnumerable<Coupon>> GetAllWithUsageAsync()
        {
            return await _dbSet
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task<bool> HasUserUsedCouponAsync(int couponId, int userId)
        {
            return await _context.CouponUsages
                .AnyAsync(u => u.CouponId == couponId && u.UserId == userId);
        }

        public async Task AddCouponUsageAsync(CouponUsage usage)
        {
            await _context.CouponUsages.AddAsync(usage);
        }

        /// <summary>
        /// Atomic increment bằng ExecuteUpdateAsync để tránh race condition
        /// khi nhiều đơn hàng cùng áp một mã.
        /// </summary>
        public async Task IncrementUsageCountAsync(int couponId)
        {
            await _dbSet
                .Where(c => c.Id == couponId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsageCount, c => c.UsageCount + 1));
        }
    }
}
