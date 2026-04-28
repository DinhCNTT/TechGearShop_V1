namespace TechGearShop_V1.Models.Entities
{
    /// <summary>Một dòng sản phẩm bên trong giỏ hàng.</summary>
    public class CartItemEntity
    {
        public int Id { get; set; }

        // FK → CartEntity
        public int CartId { get; set; }
        public CartEntity Cart { get; set; } = null!;

        // FK → Product
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }

        /// <summary>Snapshot giá lúc user thêm vào giỏ. Nếu admin đổi giá, giỏ hàng vẫn giữ giá cũ đến khi user load lại.</summary>
        public decimal UnitPrice { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
