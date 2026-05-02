using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Models.ViewModels
{
    public class OrderListViewModel
    {
        public IEnumerable<Order> Orders { get; set; } = new List<Order>();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public string? SearchKeyword { get; set; }
        public OrderStatus? Status { get; set; }
    }
}
