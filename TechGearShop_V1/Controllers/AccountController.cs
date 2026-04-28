using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechGearShop_V1.Extensions;
using TechGearShop_V1.Models.Entities;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICartService _cartService;
        private readonly INotificationService _notificationService;
        private readonly IOrderService _orderService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserService userService, ICartService cartService, INotificationService notificationService, IOrderService orderService, ILogger<AccountController> logger)
        {
            _userService = userService;
            _cartService = cartService;
            _notificationService = notificationService;
            _orderService = orderService;
            _logger      = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập rồi → redirect về trang chủ
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userService.AuthenticateAsync(model.Username, model.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                _logger.LogWarning("Failed login attempt for username: {Username}", model.Username);
                return View(model);
            }

            if (user.IsLocked)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đang bị khóa. Vui lòng liên hệ hỗ trợ.");
                return View(model);
            }

            // Build Claims Identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", user.Username),
                new Claim("Points", user.Points.ToString()),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,        // "Ghi nhớ đăng nhập"
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)  // RememberMe: 30 ngày
                    : DateTimeOffset.UtcNow.AddHours(8)  // Session thường: 8 tiếng
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User {Username} logged in successfully.", user.Username);

            // ── Merge giỏ hàng guest (Session) vào DB ──
            var sessionCart = HttpContext.Session.Get<List<CartItem>>(CartController.CART_KEY);
            if (sessionCart != null && sessionCart.Any())
            {
                await _cartService.MergeSessionCartAsync(user.Id, sessionCart);
                HttpContext.Session.Remove(CartController.CART_KEY);
            }

            // Safe redirect: chỉ redirect về URL local để chống Open Redirect Attack
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            // Admin → dashboard, Customer → Trang chủ
            return user.Role == UserRole.Admin
                ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
                : RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var newUser = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim().ToLower(),
                PhoneNumber = model.PhoneNumber.Trim(),
                Role = UserRole.Customer,
            };

            bool success = await _userService.RegisterAsync(newUser, model.Password);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email đã tồn tại. Vui lòng thử lại.");
                return View(model);
            }

            _logger.LogInformation("New user registered: {Username}", model.Username);
            
            // Bắn thông báo
            await _notificationService.CreateNotificationAsync(
                newUser.Id,
                TechGearShop_V1.Models.Enums.NotificationType.Account,
                "Chào mừng đến với TechGearShop! 🎉",
                "Khám phá ngay hàng ngàn deal công nghệ xịn sò. Bắt đầu mua sắm ngay thôi!",
                "/Product"
            );

            TempData["UserSuccess"] = "Đăng ký thành công! Hãy đăng nhập để tiếp tục mua sắm.";
            return RedirectToAction(nameof(Login));
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Clear session giỏ hàng sau khi logout
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied() => View();

        // GET: /Account/Orders
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return View(orders);
        }

        // GET: /Account/OrderDetail/{id}
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _orderService.GetOrderWithDetailsAsync(id);
            if (order == null || order.UserId != userId)
            {
                TempData["UserError"] = "Đơn hàng không tồn tại hoặc bạn không có quyền xem.";
                return RedirectToAction(nameof(Orders));
            }
            return View(order);
        }

        // GET: /Account/Notifications
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            // Fetch top 50 recent notifications. NotificationService already does this via API, but we need it here.
            // Wait, we can just return a View and let JS fetch it, or inject INotificationService.
            // Since INotificationService isn't injected in AccountController, I'll just return View and use JS.
            return View();
        }

        // POST: /Account/CancelOrder/{id}
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            bool success = await _orderService.CancelOrderAsync(id, userId);
            if (success)
                TempData["UserSuccess"] = $"Đã hủy đơn hàng #{id} thành công.";
            else
                TempData["UserError"] = "Không thể hủy đơn hàng này. Chỉ có thể hủy khi đơn đang Chờ xác nhận.";
            return RedirectToAction(nameof(Orders));
        }
    }
}
