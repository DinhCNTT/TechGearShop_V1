namespace TechGearShop_V1.Models.ViewModels
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        
        // Cảnh báo nếu số lượng khách đặt lớn hơn số tồn kho
        public bool IsOutOfStock { get; set; } 
    }
}
