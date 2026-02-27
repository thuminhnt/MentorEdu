namespace MentorEdu.Services.Interfaces
{
    public class VNPayPaymentResponse
    {
        public string? PaymentUrl { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public interface IVNPayPaymentService
    {
        /// <summary>
        /// Create a VNPay payment URL for the user to redirect to
        /// </summary>
        string CreatePaymentUrl(string orderId, long amount, string orderInfo, string clientIpAddress, string? returnUrl = null);

        /// <summary>
        /// Validate the HMAC-SHA512 signature from VNPay callback/IPN
        /// </summary>
        bool ValidateCallback(IQueryCollection queryParams);

        /// <summary>
        /// Parse VNPay return/IPN query parameters into a dictionary
        /// </summary>
        Dictionary<string, string> ParseCallbackData(IQueryCollection queryParams);
    }
}
