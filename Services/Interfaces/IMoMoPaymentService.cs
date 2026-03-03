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
        /// Tạo yêu cầu thanh toán MoMo và lấy URL thanh toán
        /// </summary>
        Task<MoMoPaymentResponse> CreatePaymentAsync(string orderId, long amount, string orderInfo, string? returnUrl = null);

        /// <summary>
        /// Xác thực chữ ký HMAC-SHA256 từ callback/IPN của MoMo
        /// </summary>
        bool ValidateCallback(Dictionary<string, string> callbackData, string signature);
    }
}
