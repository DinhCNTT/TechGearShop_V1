using System.ComponentModel.DataAnnotations.Schema;

namespace TechGearShop_V1.Models.Entities
{
    public class OrderDetail
    {
        public int Id { get; set; }

        // FK -> Order
        public int OrderId { get; set; }

        // FK -> Product
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        // Giá tại thời điểm đặt hàng (snapshot, không bị ảnh hưởng khi Product.Price thay đổi)
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
