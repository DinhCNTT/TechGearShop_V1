using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponController : Controller
    {
        private readonly ICouponService _couponService;

        public CouponController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        // GET: /Admin/Coupon
        public async Task<IActionResult> Index(string? search, string? filter)
        {
            var all = await _couponService.GetAllCouponsAsync();

            // Filter by status
            if (filter == "active")
                all = all.Where(c => c.IsCurrentlyValid);
            else if (filter == "expired")
                all = all.Where(c => c.IsExpired);
            else if (filter == "inactive")
                all = all.Where(c => !c.IsActive);

            // Search by code or description
            if (!string.IsNullOrWhiteSpace(search))
                all = all.Where(c =>
                    c.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.Description != null && c.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));

            ViewBag.Search = search;
            ViewBag.Filter = filter;
            return View(all);
        }

        // GET: /Admin/Coupon/Create
        public IActionResult Create() => View(new CouponFormDto());

        // POST: /Admin/Coupon/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CouponFormDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
                await _couponService.CreateCouponAsync(dto);
                TempData["SuccessMessage"] = $"Đã tạo mã giảm giá \"{dto.Code.ToUpperInvariant()}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message.Contains("UNIQUE")
                    ? "Mã này đã tồn tại. Vui lòng chọn mã khác."
                    : "Có lỗi xảy ra khi tạo mã. Vui lòng thử lại.");
                return View(dto);
            }
        }

        // GET: /Admin/Coupon/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _couponService.GetByIdAsync(id);
            if (coupon == null) return NotFound();

            var dto = new CouponFormDto
            {
                Code             = coupon.Code,
                Description      = coupon.Description,
                DiscountType     = coupon.DiscountType,
                DiscountValue    = coupon.DiscountValue,
                MaxDiscountAmount = coupon.MaxDiscountAmount,
                MinOrderValue    = coupon.MinOrderValue,
                StartDate        = coupon.StartDate.ToLocalTime(),
                ExpiryDate       = coupon.ExpiryDate.ToLocalTime(),
                UsageLimit       = coupon.UsageLimit,
                IsActive         = coupon.IsActive
            };

            ViewBag.CouponId = id;
            ViewBag.UsageCount = coupon.UsageCount;
            return View(dto);
        }

        // POST: /Admin/Coupon/Edit/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CouponFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CouponId = id;
                return View(dto);
            }

            try
            {
                await _couponService.UpdateCouponAsync(id, dto);
                TempData["SuccessMessage"] = $"Đã cập nhật mã \"{dto.Code.ToUpperInvariant()}\".";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message.Contains("UNIQUE")
                    ? "Mã này đã tồn tại. Vui lòng chọn mã khác."
                    : "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại.");
                ViewBag.CouponId = id;
                return View(dto);
            }
        }

        // POST: /Admin/Coupon/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _couponService.DeleteCouponAsync(id);
                TempData["SuccessMessage"] = "Đã xóa mã giảm giá.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể xóa mã này.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Coupon/Toggle/{id} — AJAX
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            try
            {
                bool isActive = await _couponService.ToggleActiveAsync(id);
                var coupon = await _couponService.GetByIdAsync(id);

                // Tính trạng thái thực tế sau khi toggle để trả về cho UI
                string statusLabel;
                string statusCss;

                if (!isActive)
                {
                    statusLabel = "Đã tắt"; statusCss = "bg-slate";
                }
                else if (coupon!.IsNotStarted)
                {
                    statusLabel = "Chưa bắt đầu"; statusCss = "bg-violet";
                }
                else if (coupon.IsExpired)
                {
                    statusLabel = "Hết hạn"; statusCss = "bg-red";
                }
                else if (coupon.IsExhausted)
                {
                    statusLabel = "Hết lượt"; statusCss = "bg-orange";
                }
                else
                {
                    statusLabel = "Đang hoạt động"; statusCss = "bg-green";
                }

                return Json(new { success = true, isActive, statusLabel, statusCss });
            }
            catch
            {
                return Json(new { success = false });
            }
        }
    }
}
