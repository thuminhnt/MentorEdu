using System.Text;
using System.Text.Json;
using MentorEdu.Models;
using MentorEdu.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MentorEdu.Services.Implementations
{
    public class VietQRService : IVietQRService
    {
        private readonly VietQRSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<VietQRService> _logger;

        public VietQRService(
            IOptions<VietQRSettings> settings,
            HttpClient httpClient,
            ILogger<VietQRService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<VietQRResponse> GenerateQRAsync(decimal amount, string description)
        {
            try
            {
                // Use VietQR Quick Link (no API key required)
                // Format: https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={amount}&addInfo={description}&accountName={name}
                var encodedDesc = Uri.EscapeDataString(description);
                var encodedName = Uri.EscapeDataString(_settings.AccountName);

                var qrUrl = $"https://img.vietqr.io/image/{_settings.BankId}-{_settings.AccountNo}-{_settings.Template}.png" +
                            $"?amount={(long)amount}&addInfo={encodedDesc}&accountName={encodedName}";

                _logger.LogInformation("VietQR Generated - Amount: {Amount}, Description: {Description}, URL: {Url}",
                    amount, description, qrUrl);

                return Task.FromResult(new VietQRResponse
                {
                    QrDataUrl = qrUrl,
                    BankId = _settings.BankId,
                    AccountNo = _settings.AccountNo,
                    AccountName = _settings.AccountName,
                    Amount = amount,
                    Description = description,
                    Success = true,
                    Message = "QR code generated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VietQR generation failed");
                return Task.FromResult(new VietQRResponse
                {
                    Success = false,
                    Message = $"Failed to generate QR: {ex.Message}"
                });
            }
        }
    }
}
