using Microsoft.AspNetCore.Http;
using TechGearShop_V1.Models.DTOs;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, HttpContext httpContext);
        VNPayResponseDto ValidatePaymentResponse(IQueryCollection queryParams);
    }
}
