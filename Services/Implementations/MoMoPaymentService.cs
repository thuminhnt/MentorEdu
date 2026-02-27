using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MentorEdu.Models;
using MentorEdu.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MentorEdu.Services.Implementations
{
    public class MoMoPaymentService : IMoMoPaymentService
    {
        private readonly MoMoSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MoMoPaymentService> _logger;

        public MoMoPaymentService(
            IOptions<MoMoSettings> settings,
            HttpClient httpClient,
            ILogger<MoMoPaymentService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<MoMoPaymentResponse> CreatePaymentAsync(
            string orderId, long amount, string orderInfo, string? returnUrl = null)
        {
            var requestId = Guid.NewGuid().ToString();
            var redirectUrl = returnUrl ?? _settings.RedirectUrl;
            var ipnUrl = _settings.IpnUrl;
            var extraData = ""; // Base64 encoded extra data, empty for now

            // Build raw signature string (alphabetical order as per MoMo docs)
            var rawSignature = $"accessKey={_settings.AccessKey}" +
                               $"&amount={amount}" +
                               $"&extraData={extraData}" +
                               $"&ipnUrl={ipnUrl}" +
                               $"&orderId={orderId}" +
                               $"&orderInfo={orderInfo}" +
                               $"&partnerCode={_settings.PartnerCode}" +
                               $"&redirectUrl={redirectUrl}" +
                               $"&requestId={requestId}" +
                               $"&requestType={_settings.RequestType}";

            var signature = ComputeHmacSha256(rawSignature, _settings.SecretKey);

            _logger.LogInformation("MoMo CreatePayment - OrderId: {OrderId}, Amount: {Amount}, Signature: {Signature}",
                orderId, amount, signature);

            // Build request body
            var requestBody = new
            {
                partnerCode = _settings.PartnerCode,
                accessKey = _settings.AccessKey,
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                extraData = extraData,
                requestType = _settings.RequestType,
                signature = signature,
                lang = "vi"
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_settings.ApiEndpoint, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("MoMo Response: {Response}", responseContent);

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                var result = new MoMoPaymentResponse
                {
                    ResultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1,
                    Message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error",
                    PayUrl = root.TryGetProperty("payUrl", out var pu) ? pu.GetString() : null,
                    QrCodeUrl = root.TryGetProperty("qrCodeUrl", out var qr) ? qr.GetString() : null,
                    Deeplink = root.TryGetProperty("deeplink", out var dl) ? dl.GetString() : null,
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MoMo CreatePayment failed for OrderId: {OrderId}", orderId);
                return new MoMoPaymentResponse
                {
                    ResultCode = -1,
                    Message = $"Failed to connect to MoMo: {ex.Message}"
                };
            }
        }

        public bool ValidateCallback(Dictionary<string, string> callbackData, string signature)
        {
            // Build raw signature from callback data (alphabetical order)
            // MoMo IPN signature format:
            // accessKey=$accessKey&amount=$amount&extraData=$extraData
            // &message=$message&orderId=$orderId&orderInfo=$orderInfo
            // &orderType=$orderType&partnerCode=$partnerCode&payType=$payType
            // &requestId=$requestId&responseTime=$responseTime
            // &resultCode=$resultCode&transId=$transId

            var rawSignature = $"accessKey={GetValue(callbackData, "accessKey")}" +
                               $"&amount={GetValue(callbackData, "amount")}" +
                               $"&extraData={GetValue(callbackData, "extraData")}" +
                               $"&message={GetValue(callbackData, "message")}" +
                               $"&orderId={GetValue(callbackData, "orderId")}" +
                               $"&orderInfo={GetValue(callbackData, "orderInfo")}" +
                               $"&orderType={GetValue(callbackData, "orderType")}" +
                               $"&partnerCode={GetValue(callbackData, "partnerCode")}" +
                               $"&payType={GetValue(callbackData, "payType")}" +
                               $"&requestId={GetValue(callbackData, "requestId")}" +
                               $"&responseTime={GetValue(callbackData, "responseTime")}" +
                               $"&resultCode={GetValue(callbackData, "resultCode")}" +
                               $"&transId={GetValue(callbackData, "transId")}";

            var computedSignature = ComputeHmacSha256(rawSignature, _settings.SecretKey);

            _logger.LogInformation("MoMo ValidateCallback - Raw: {Raw}, Computed: {Computed}, Received: {Received}",
                rawSignature, computedSignature, signature);

            return string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static string GetValue(Dictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value : "";
        }
    }
}
