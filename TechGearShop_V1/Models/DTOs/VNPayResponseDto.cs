namespace TechGearShop_V1.Models.DTOs
{
    public class VNPayResponseDto
    {
        public bool IsSuccess { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
    }
}
