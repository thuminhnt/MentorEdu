using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Wallet;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Get current user's wallet
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
        /// Get wallet detail with recent transactions
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
        /// Get wallet summary
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
        /// Top up wallet
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
        /// Process topup callback from payment gateway
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
        /// Request withdrawal
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
        /// Get transaction history
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
