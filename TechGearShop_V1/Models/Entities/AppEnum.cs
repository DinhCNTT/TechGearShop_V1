namespace TechGearShop_V1.Models.Entities
{
    public enum UserRole
    {
        Customer = 0,
        Admin = 1
    }

    public enum OrderStatus
    {
        Pending = 0,      // Chờ duyệt
        Processing = 1,   // Đang xử lý
        Shipping = 2,     // Đang giao
        Completed = 3,    // Thành công
        Cancelled = 4     // Đã hủy
    }

    public enum PaymentMethod
    {
        COD = 0,      // Thanh toán khi nhận hàng
        VNPay = 1,    // Thanh toán qua VNPay
        MoMo = 2      // Thanh toán qua MoMo
    }
}
