namespace TechGearShop_V1.Models.Entities
{
    public enum UserRole
    {
        Customer = 0,
        Admin = 1
    }

    public enum OrderStatus
    {
        PaymentPending = -1, // Chờ thanh toán online (VNPay/MoMo)
        Pending = 0,      // Chờ duyệt
        Processing = 1,   // Đang xử lý
        Shipping = 2,     // Đang giao
        Completed = 3,    // Thành công
        Cancelled = 4     // Đã hủy
    }

    public enum PaymentStatus
    {
        Unpaid = 0,   // Chưa thanh toán
        Paid = 1,     // Đã thanh toán thành công
        Failed = 2,   // Thanh toán thất bại
        Refunded = 3  // Đã hoàn tiền
    }

    public enum PaymentMethod
    {
        COD = 0,      // Thanh toán khi nhận hàng
        VNPay = 1,    // Thanh toán qua VNPay
        MoMo = 2      // Thanh toán qua MoMo
    }
}
