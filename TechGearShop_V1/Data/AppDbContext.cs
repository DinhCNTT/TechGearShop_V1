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
        public DbSet<StockSubscription> StockSubscriptions { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ProductQuestion> ProductQuestions { get; set; }

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

            // Fix decimal precision: tránh cắt bớt số thập phân khi lưu tiền
            modelBuilder.Entity<CartItemEntity>()
                .Property(ci => ci.UnitPrice)
                .HasPrecision(18, 2);

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

            // === Bảo vệ tồn kho ở tầng Database (lớp phòng thủ cuối cùng) ===
            // Dù code có bug hay bị bypass, SQL Server tuyệt đối không cho Stock xuống âm.
            modelBuilder.Entity<Product>()
                .ToTable(tb => tb.HasCheckConstraint("CHK_Products_Stock_NonNegative", "[Stock] >= 0"));

            // === Review ===
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Hoặc NoAction tùy rule

            // Đảm bảo Rating nằm trong khoảng 1-5 bằng Check Constraint
            modelBuilder.Entity<Review>()
                .ToTable(tb => tb.HasCheckConstraint("CHK_Review_Rating", "[Rating] >= 1 AND [Rating] <= 5"));

            // === StockSubscription ===
            modelBuilder.Entity<StockSubscription>()
                .HasIndex(s => new { s.ProductId, s.Status })
                .HasDatabaseName("IX_StockSubscriptions_Product_Status");

            modelBuilder.Entity<StockSubscription>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockSubscription>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Keep the subscription if user deleted

            // === ProductQuestion (Hỏi đáp nhanh) ===
            modelBuilder.Entity<ProductQuestion>()
                .HasIndex(q => new { q.ProductId, q.CreatedAt })
                .HasDatabaseName("IX_ProductQuestions_Product_CreatedAt");

            modelBuilder.Entity<ProductQuestion>()
                .HasOne(q => q.Product)
                .WithMany()
                .HasForeignKey(q => q.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductQuestion>()
                .HasOne(q => q.User)
                .WithMany()
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
