using System.ComponentModel.DataAnnotations;
using TechGearShop_V1.Models.Entities;

namespace TechGearShop_V1.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
        [StringLength(100, ErrorMessage = "Tên không được quá 100 ký tự.")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15, ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số nhà, tên đường, phường/xã.")]
        [StringLength(200, ErrorMessage = "Địa chỉ nhận hàng quá dài.")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố.")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        public string? CouponCode { get; set; }
        
        public string? Note { get; set; }

        // Data for rendering the summary
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal SubTotal => CartItems.Sum(x => x.Price * x.Quantity);
        public decimal ShippingFee { get; set; } = 0; // Configurable based on Province
        public decimal DiscountAmount { get; set; } = 0;
        public decimal FinalTotal => SubTotal + ShippingFee - DiscountAmount;
    }
}
