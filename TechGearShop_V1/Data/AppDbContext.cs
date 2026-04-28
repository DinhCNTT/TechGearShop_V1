using Microsoft.EntityFrameworkCore;
using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<CartEntity> Carts { get; set; }
        public DbSet<CartItemEntity> CartItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === Index để tăng hiệu năng truy vấn ===
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CategoryId)
                .HasDatabaseName("IX_Products_CategoryId");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Brand)
                .HasDatabaseName("IX_Products_Brand");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.UserId)
                .HasDatabaseName("IX_Orders_UserId");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status)
                .HasDatabaseName("IX_Orders_Status");

            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique()
                .HasDatabaseName("IX_Coupons_Code");

            // Cart: mỗi User có tối đa 1 Cart
            modelBuilder.Entity<CartEntity>()
                .HasIndex(c => c.UserId)
                .IsUnique()
                .HasDatabaseName("IX_Carts_UserId");

            // Cart → User
            modelBuilder.Entity<CartEntity>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem → Cart
            modelBuilder.Entity<CartItemEntity>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem → Product
            modelBuilder.Entity<CartItemEntity>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Không xóa sản phẩm nếu còn trong giỏ

            // === Quan hệ (Relationships) ===

            // Product -> Category (Nhiều Product thuộc 1 Category)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Không xóa Category nếu còn Product

            // Order -> User (Nhiều Order thuộc 1 User)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderDetail -> Order
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderDetail -> Product
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification -> User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
