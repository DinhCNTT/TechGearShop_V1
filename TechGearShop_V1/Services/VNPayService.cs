using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly VNPaySettings _config;

        public VNPayService(IOptions<VNPaySettings> config)
        {
            _config = config.Value;
        }

        public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, HttpContext httpContext)
        {
            var vnpayData = new SortedList<string, string>(new VNPayCompare());

            // Get IP Address
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1") ipAddress = "127.0.0.1";

            vnpayData.Add("vnp_Version", _config.Version);
            vnpayData.Add("vnp_Command", _config.Command);
            vnpayData.Add("vnp_TmnCode", _config.TmnCode);
            // Amount in VNPay must be multiplied by 100
            vnpayData.Add("vnp_Amount", ((long)(amount * 100)).ToString());
            vnpayData.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpayData.Add("vnp_CurrCode", _config.CurrCode);
            vnpayData.Add("vnp_IpAddr", ipAddress);
            vnpayData.Add("vnp_Locale", _config.Locale);
            vnpayData.Add("vnp_OrderInfo", orderInfo);
            vnpayData.Add("vnp_OrderType", "other");
            vnpayData.Add("vnp_ReturnUrl", _config.ReturnUrl);
            vnpayData.Add("vnp_TxnRef", orderId.ToString());

            // Build query string
            var data = new StringBuilder();
            foreach (var kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            var queryString = data.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(queryString.Length - 1, 1); // remove last '&'
            }

            // Create signature
            var vnp_SecureHash = HmacSHA512(_config.HashSecret, queryString);
            var paymentUrl = _config.BaseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;

            return paymentUrl;
        }

        public VNPayResponseDto ValidatePaymentResponse(IQueryCollection queryParams)
        {
            var vnpayData = new SortedList<string, string>(new VNPayCompare());
            var vnp_SecureHash = string.Empty;

            foreach (var kv in queryParams)
            {
                if (kv.Key.StartsWith("vnp_"))
                {
                    if (kv.Key == "vnp_SecureHash")
                    {
                        vnp_SecureHash = kv.Value;
                    }
                    else
                    {
                        vnpayData.Add(kv.Key, kv.Value.ToString());
                    }
                }
            }

            // Build query string
            var data = new StringBuilder();
            foreach (var kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            var queryString = data.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(queryString.Length - 1, 1);
            }

            var checkSum = HmacSHA512(_config.HashSecret, queryString);
            var isSuccess = checkSum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);

            if (!isSuccess)
            {
                return new VNPayResponseDto { IsSuccess = false, ResponseCode = "99" }; // Invalid signature
            }

            var responseCode = queryParams["vnp_ResponseCode"].ToString();
            
            return new VNPayResponseDto
            {
                IsSuccess = responseCode == "00",
                OrderId = queryParams["vnp_TxnRef"].ToString(),
                Amount = decimal.Parse(queryParams["vnp_Amount"].ToString()) / 100,
                TransactionId = queryParams["vnp_TransactionNo"].ToString(),
                ResponseCode = responseCode,
                BankCode = queryParams["vnp_BankCode"].ToString()
            };
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }
    }

    public class VNPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = string.Compare(x, y, StringComparison.Ordinal);
            if (vnpCompare != 0) return vnpCompare;
            return string.Compare(x, y, StringComparison.Ordinal);
        }
    }
}
