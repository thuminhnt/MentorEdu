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
        /// Tạo URL thanh toán VNPay để người dùng chuyển hướng đến
        /// </summary>
        string CreatePaymentUrl(string orderId, long amount, string orderInfo, string clientIpAddress, string? returnUrl = null);

        /// <summary>
        /// Xác thực chữ ký HMAC-SHA512 từ callback/IPN của VNPay
        /// </summary>
        bool ValidateCallback(IQueryCollection queryParams);

        /// <summary>
        /// Phân tích tham số query từ trang trả về/IPN của VNPay thành dictionary
        /// </summary>
        Dictionary<string, string> ParseCallbackData(IQueryCollection queryParams);
    }
}
