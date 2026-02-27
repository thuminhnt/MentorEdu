namespace MentorEdu.Models
{
    public class VietQRSettings
    {
        public string BankId { get; set; } = string.Empty;       // VietQR bank code (e.g. "MB")
        public string AccountNo { get; set; } = string.Empty;    // Bank account number
        public string AccountName { get; set; } = string.Empty;  // Account holder name
        public string Template { get; set; } = "compact2";       // QR template: compact, compact2, qr_only
    }
}
