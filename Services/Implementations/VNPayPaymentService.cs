using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MentorEdu.Models;
using MentorEdu.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MentorEdu.Services.Implementations
{
    public class VNPayPaymentService : IVNPayPaymentService
    {
        private readonly VNPaySettings _settings;
        private readonly ILogger<VNPayPaymentService> _logger;

        public VNPayPaymentService(
            IOptions<VNPaySettings> settings,
            ILogger<VNPayPaymentService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public string CreatePaymentUrl(string orderId, long amount, string orderInfo, string clientIpAddress, string? returnUrl = null)
        {
            var redirectUrl = returnUrl ?? _settings.ReturnUrl;
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var expireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss");

            // Build VNPay request parameters (sorted alphabetically)
            var vnpParams = new SortedDictionary<string, string>
            {
                { "vnp_Version", _settings.Version },
                { "vnp_Command", _settings.Command },
                { "vnp_TmnCode", _settings.TmnCode },
                { "vnp_Amount", (amount * 100).ToString() }, // VNPay amount is in smallest currency unit (x100 for VND)
                { "vnp_CreateDate", createDate },
                { "vnp_CurrCode", _settings.CurrCode },
                { "vnp_IpAddr", clientIpAddress },
                { "vnp_Locale", _settings.Locale },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "topup" },
                { "vnp_ReturnUrl", redirectUrl },
                { "vnp_TxnRef", orderId },
                { "vnp_ExpireDate", expireDate }
            };

            // Build query string and hash data
            // VNPay official SDK: hash data = URL-encoded query string (same encoding for both)
            var queryBuilder = new StringBuilder();

            foreach (var kvp in vnpParams)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    queryBuilder.Append(WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value) + "&");
                }
            }

            var queryString = queryBuilder.ToString();
            // Hash data = query string without trailing '&'
            var hashData = queryString.TrimEnd('&');

            // Compute HMAC-SHA512 signature
            var secureHash = ComputeHmacSha512(hashData, _settings.HashSecret);
            var paymentUrl = $"{_settings.PaymentUrl}?{queryString}vnp_SecureHash={secureHash}";

            _logger.LogInformation("VNPay CreatePayment - OrderId: {OrderId}, Amount: {Amount}, URL: {Url}",
                orderId, amount, paymentUrl);

            return paymentUrl;
        }

        public bool ValidateCallback(IQueryCollection queryParams)
        {
            if (!queryParams.ContainsKey("vnp_SecureHash"))
                return false;

            var receivedHash = queryParams["vnp_SecureHash"].ToString();

            // Build hash data from sorted params (exclude vnp_SecureHash and vnp_SecureHashType)
            // Must URL-encode values to match VNPay's hash computation
            var sortedParams = new SortedDictionary<string, string>();
            foreach (var key in queryParams.Keys)
            {
                if (key != "vnp_SecureHash" && key != "vnp_SecureHashType" && !string.IsNullOrEmpty(queryParams[key]))
                {
                    sortedParams[key] = queryParams[key].ToString();
                }
            }

            var hashDataBuilder = new StringBuilder();
            foreach (var kvp in sortedParams)
            {
                hashDataBuilder.Append(WebUtility.UrlEncode(kvp.Key) + "=" + WebUtility.UrlEncode(kvp.Value) + "&");
            }
            var hashData = hashDataBuilder.ToString().TrimEnd('&');

            var computedHash = ComputeHmacSha512(hashData, _settings.HashSecret);

            _logger.LogInformation("VNPay ValidateCallback - HashData: {HashData}, Computed: {Computed}, Received: {Received}",
                hashData, computedHash, receivedHash);

            return string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> ParseCallbackData(IQueryCollection queryParams)
        {
            var data = new Dictionary<string, string>();
            foreach (var key in queryParams.Keys)
            {
                data[key] = queryParams[key].ToString();
            }
            return data;
        }

        private static string ComputeHmacSha512(string data, string key)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
