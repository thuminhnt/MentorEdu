namespace MentorEdu.Services.Interfaces
{
    public class MoMoPaymentResponse
    {
        public string? PayUrl { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? Deeplink { get; set; }
        public int ResultCode { get; set; }
        public string? Message { get; set; }
    }

    public interface IMoMoPaymentService
    {
        /// <summary>
        /// Create a MoMo payment request and get the payment URL
        /// </summary>
        Task<MoMoPaymentResponse> CreatePaymentAsync(string orderId, long amount, string orderInfo, string? returnUrl = null);

        /// <summary>
        /// Validate the HMAC-SHA256 signature from MoMo callback/IPN
        /// </summary>
        bool ValidateCallback(Dictionary<string, string> callbackData, string signature);
    }
}
