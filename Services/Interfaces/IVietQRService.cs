namespace MentorEdu.Services.Interfaces
{
    public class VietQRResponse
    {
        public string? QrDataUrl { get; set; }     // Ảnh QR dạng Base64 hoặc URL
        public string? BankId { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountName { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public interface IVietQRService
    {
        /// <summary>
        /// Tạo mã QR chuyển khoản ngân hàng VietQR
        /// </summary>
        Task<VietQRResponse> GenerateQRAsync(decimal amount, string description);
    }
}
