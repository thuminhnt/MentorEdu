using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Wallet;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(IWalletService walletService, ILogger<WalletController> logger)
        {
            _walletService = walletService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint kiểm tra kết nối
        /// </summary>
        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            return Ok("pong");
        }

        /// <summary>
        /// Lấy ví của người dùng hiện tại
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<WalletDto>>> GetWallet()
        {
            var userId = GetCurrentUserId();
            var result = await _walletService.GetWalletAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<WalletDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<WalletDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Lấy chi tiết ví kèm giao dịch gần đây
        /// </summary>
        [HttpGet("detail")]
        public async Task<ActionResult<ApiResponse<WalletDetailDto>>> GetWalletDetail()
        {
            var userId = GetCurrentUserId();
            var result = await _walletService.GetWalletDetailAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<WalletDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<WalletDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Lấy tóm tắt ví
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<WalletSummaryDto>>> GetWalletSummary()
        {
            var userId = GetCurrentUserId();
            var result = await _walletService.GetWalletSummaryAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<WalletSummaryDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<WalletSummaryDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Nạp tiền vào ví - tạo thanh toán MoMo và trả về URL thanh toán
        /// </summary>
        [HttpPost("topup")]
        public async Task<ActionResult<ApiResponse<TopupResponseDto>>> Topup([FromBody] TopupRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<TopupResponseDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _walletService.TopupAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<TopupResponseDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<TopupResponseDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Xử lý callback nạp tiền từ cổng thanh toán (tổng quát)
        /// </summary>
        [HttpPost("topup/callback")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> TopupCallback([FromBody] PaymentCallbackDto callback)
        {
            var result = await _walletService.ProcessTopupCallbackAsync(callback);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Callback IPN (Thông báo thanh toán tức thì) của MoMo
        /// MoMo sẽ POST đến endpoint này sau khi thanh toán hoàn tất
        /// </summary>
        [HttpPost("momo/ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> MoMoIpn()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                _logger.LogInformation("MoMo IPN received: {Body}", body);

                var ipnData = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                if (ipnData == null)
                {
                    return BadRequest(new { resultCode = -1, message = "Invalid request body" });
                }

                var result = await _walletService.ProcessMoMoIpnAsync(ipnData);

                // MoMo expects HTTP 204 No Content for successful IPN processing
                if (result.Success)
                {
                    return NoContent();
                }

                return BadRequest(new { resultCode = -1, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MoMo IPN processing error");
                return StatusCode(500, new { resultCode = -1, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Callback IPN (Thông báo thanh toán tức thì) của VNPay
        /// VNPay sẽ GET đến endpoint này sau khi thanh toán hoàn tất
        /// </summary>
        [HttpGet("vnpay/ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> VNPayIpn()
        {
            try
            {
                _logger.LogInformation("VNPay IPN received: {Query}", Request.QueryString);

                var result = await _walletService.ProcessVNPayCallbackAsync(Request.Query);

                if (result.Success)
                {
                    // VNPay expects this exact JSON response format
                    return Ok(new { RspCode = "00", Message = "Confirm Success" });
                }

                return Ok(new { RspCode = "99", Message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay IPN processing error");
                return Ok(new { RspCode = "99", Message = "Internal server error" });
            }
        }

        /// <summary>
        /// URL trả về của VNPay - Xác minh thanh toán VNPay từ frontend
        /// Frontend gọi sau khi VNPay chuyển hướng về kèm query params
        /// </summary>
        [HttpGet("vnpay/return")]
        [AllowAnonymous]
        public async Task<IActionResult> VNPayReturn()
        {
            try
            {
                _logger.LogInformation("VNPay Return/Verify received: {Query}", Request.QueryString);

                var result = await _walletService.ProcessVNPayCallbackAsync(Request.Query);

                var txnRef = Request.Query["vnp_TxnRef"].ToString();
                var responseCode = Request.Query["vnp_ResponseCode"].ToString();
                var amount = Request.Query["vnp_Amount"].ToString();
                var transNo = Request.Query["vnp_TransactionNo"].ToString();

                return Ok(new
                {
                    success = result.Success && responseCode == "00",
                    responseCode,
                    transactionRef = txnRef,
                    amount,
                    transactionNo = transNo,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay Return/Verify processing error");
                return Ok(new { success = false, message = "Processing error" });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái giao dịch theo mã tham chiếu
        /// </summary>
        [HttpGet("topup/check/{transactionRef}")]
        public async Task<ActionResult<ApiResponse<TransactionDto>>> CheckTransactionStatus(string transactionRef)
        {
            var result = await _walletService.CheckTransactionStatusAsync(transactionRef);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<TransactionDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<TransactionDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Admin: Xác nhận nạp tiền qua chuyển khoản ngân hàng
        /// </summary>
        [HttpPost("bank-transfer/confirm/{transactionRef}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> ConfirmBankTransfer(string transactionRef)
        {
            var adminUserId = GetCurrentUserId();
            var result = await _walletService.ConfirmBankTransferAsync(adminUserId, transactionRef);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Admin: Lấy danh sách nạp tiền chuyển khoản đang chờ xử lý
        /// </summary>
        [HttpGet("bank-transfer/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResponse<TransactionDto>>>> GetPendingBankTransfers(
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _walletService.GetPendingBankTransfersAsync(pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<TransactionDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<TransactionDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Admin: Từ chối nạp tiền qua chuyển khoản ngân hàng
        /// </summary>
        [HttpPost("bank-transfer/reject/{transactionRef}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> RejectBankTransfer(string transactionRef)
        {
            var adminUserId = GetCurrentUserId();
            var result = await _walletService.RejectBankTransferAsync(adminUserId, transactionRef);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Yêu cầu rút tiền
        /// </summary>
        [HttpPost("withdraw")]
        public async Task<ActionResult<ApiResponse>> Withdraw([FromBody] WithdrawRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _walletService.WithdrawAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Lấy lịch sử giao dịch
        /// </summary>
        [HttpGet("transactions")]
        public async Task<ActionResult<ApiResponse<PagedResponse<TransactionDto>>>> GetTransactions([FromQuery] SearchTransactionDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _walletService.GetTransactionsAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<TransactionDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<TransactionDto>>.SuccessResponse(result.Data!));
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
