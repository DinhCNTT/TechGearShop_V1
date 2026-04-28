namespace TechGearShop_V1.Models.Entities
{
    /// <summary>Đại diện cho một giỏ hàng gắn với một User.</summary>
    public class CartEntity
    {
        public int Id { get; set; }

        // FK → User (mỗi user có đúng 1 cart)
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<CartItemEntity> Items { get; set; } = new List<CartItemEntity>();
    }
}
