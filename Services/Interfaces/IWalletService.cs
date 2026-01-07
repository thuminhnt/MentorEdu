using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Wallet;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IWalletService
    {
        Task<ServiceResult<WalletDto>> GetWalletAsync(long userId);
        Task<ServiceResult<WalletDetailDto>> GetWalletDetailAsync(long userId);
        Task<ServiceResult<TopupResponseDto>> TopupAsync(long userId, TopupRequestDto request);
        Task<ServiceResult> ProcessTopupCallbackAsync(PaymentCallbackDto callback);
        Task<ServiceResult> WithdrawAsync(long userId, WithdrawRequestDto request);
        Task<ServiceResult<PagedResponse<TransactionDto>>> GetTransactionsAsync(long userId, SearchTransactionDto searchDto);
        Task<ServiceResult<WalletSummaryDto>> GetWalletSummaryAsync(long userId);
    }
}
