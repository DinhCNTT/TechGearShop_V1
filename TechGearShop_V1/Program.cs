using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Data;

var builder = WebApplication.CreateBuilder(args);

// === MVC ===
builder.Services.AddControllersWithViews();

// === Database — Entity Framework Core ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Authentication — Cookie-based ===
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes"));
        options.SlidingExpiration = true;
    });

// === Session (dùng cho Giỏ hàng) ===
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// === Cache (dùng cho danh mục, banner, popup) ===
builder.Services.AddMemoryCache();

// === Dependency Injection (Repositories & Services) ===
// Repositories
builder.Services.AddScoped(typeof(TechGearShop_V1.Repositories.Interfaces.IGenericRepository<>), typeof(TechGearShop_V1.Repositories.GenericRepository<>));
builder.Services.AddScoped<TechGearShop_V1.Repositories.Interfaces.IProductRepository, TechGearShop_V1.Repositories.ProductRepository>();
builder.Services.AddScoped<TechGearShop_V1.Repositories.Interfaces.ICategoryRepository, TechGearShop_V1.Repositories.CategoryRepository>();
builder.Services.AddScoped<TechGearShop_V1.Repositories.Interfaces.IOrderRepository, TechGearShop_V1.Repositories.OrderRepository>();
builder.Services.AddScoped<TechGearShop_V1.Repositories.Interfaces.IUserRepository, TechGearShop_V1.Repositories.UserRepository>();
builder.Services.AddScoped<TechGearShop_V1.Repositories.Interfaces.ICouponRepository, TechGearShop_V1.Repositories.CouponRepository>();

// Services
builder.Services.AddScoped<TechGearShop_V1.Services.Interfaces.IProductService, TechGearShop_V1.Services.ProductService>();
builder.Services.AddScoped<TechGearShop_V1.Services.Interfaces.ICategoryService, TechGearShop_V1.Services.CategoryService>();
builder.Services.AddScoped<TechGearShop_V1.Services.Interfaces.IOrderService, TechGearShop_V1.Services.OrderService>();
builder.Services.AddScoped<TechGearShop_V1.Services.Interfaces.IUserService, TechGearShop_V1.Services.UserService>();
builder.Services.AddScoped<TechGearShop_V1.Services.Interfaces.ICouponService, TechGearShop_V1.Services.CouponService>();
builder.Services.AddScoped<TechGearShop_V1.Services.Interfaces.IImageService, TechGearShop_V1.Services.ImageService>();

var app = builder.Build();

// === Pipeline ===
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // Phải đứng trước UseAuthorization
app.UseAuthorization();

app.UseSession(); // Phải đứng sau UseRouting

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

