using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Wallet;
using MentorEdu.Services.Common;
using Microsoft.AspNetCore.Http;

namespace MentorEdu.Services.Interfaces
{
    public interface IWalletService
    {
        Task<ServiceResult<WalletDto>> GetWalletAsync(long userId);
        Task<ServiceResult<WalletDetailDto>> GetWalletDetailAsync(long userId);
        Task<ServiceResult<TopupResponseDto>> TopupAsync(long userId, TopupRequestDto request);
        Task<ServiceResult> ProcessTopupCallbackAsync(PaymentCallbackDto callback);
        Task<ServiceResult> ProcessMoMoIpnAsync(Dictionary<string, object> ipnData);
        Task<ServiceResult> ProcessVNPayCallbackAsync(IQueryCollection queryParams);
        Task<ServiceResult> ConfirmBankTransferAsync(long adminUserId, string transactionRef);
        Task<ServiceResult> RejectBankTransferAsync(long adminUserId, string transactionRef);
        Task<ServiceResult<PagedResponse<TransactionDto>>> GetPendingBankTransfersAsync(int pageNumber = 1, int pageSize = 20);
        Task<ServiceResult> WithdrawAsync(long userId, WithdrawRequestDto request);
        Task<ServiceResult<PagedResponse<TransactionDto>>> GetTransactionsAsync(long userId, SearchTransactionDto searchDto);
        Task<ServiceResult<WalletSummaryDto>> GetWalletSummaryAsync(long userId);
        Task<ServiceResult<TransactionDto>> CheckTransactionStatusAsync(string transactionRef);
    }
}
