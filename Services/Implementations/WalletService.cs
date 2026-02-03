using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Wallet;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class WalletService : BaseService, IWalletService
    {
        public WalletService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public async Task<ServiceResult<WalletDto>> GetWalletAsync(long userId)
        {
            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            
            if (wallet == null)
            {
                // Create wallet if not exists
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    LockedBalance = 0,
                    Currency = "VND",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Wallets.AddAsync(wallet);
                await _unitOfWork.SaveChangesAsync();
            }

            var dto = MapToWalletDto(wallet);
            return ServiceResult<WalletDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<WalletDetailDto>> GetWalletDetailAsync(long userId)
        {
            var walletResult = await GetWalletAsync(userId);
            if (!walletResult.Success)
                return ServiceResult<WalletDetailDto>.FailResult(walletResult.Message ?? "");

            var wallet = await _unitOfWork.Wallets.GetByUserIdWithTransactionsAsync(userId);
            var summary = await CalculateWalletSummary(wallet!.Id);

            var dto = new WalletDetailDto
            {
                Wallet = walletResult.Data,
                RecentTransactions = wallet.Transactions?
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .Select(MapToTransactionDto)
                    .ToList(),
                Summary = summary
            };

            return ServiceResult<WalletDetailDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult<TopupResponseDto>> TopupAsync(long userId, TopupRequestDto request)
        {
            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            
            if (wallet == null)
            {
                var createResult = await GetWalletAsync(userId);
                if (!createResult.Success)
                    return ServiceResult<TopupResponseDto>.FailResult("Cannot create wallet");
                
                wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            }

            // Create pending transaction
            var transactionRef = $"TOP_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 50);

            var transaction = new Transaction
            {
                WalletId = wallet!.Id,
                Type = TransactionType.Topup,
                Amount = request.Amount,
                ExternalRef = transactionRef,
                Status = TransactionStatus.Pending,
                Description = $"Top up via {request.PaymentMethod}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            // TODO: Integrate with actual payment gateway (VNPay, Momo, etc.)
            // For now, return mock payment URL
            var paymentUrl = $"https://payment.example.com/pay?ref={transactionRef}&amount={request.Amount}";

            var response = new TopupResponseDto
            {
                PaymentUrl = paymentUrl,
                TransactionRef = transactionRef,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                ExpireAt = DateTime.UtcNow.AddMinutes(15)
            };

            return ServiceResult<TopupResponseDto>.SuccessResult(response, "Please complete the payment");
        }

        public async Task<ServiceResult> ProcessTopupCallbackAsync(PaymentCallbackDto callback)
        {
            if (string.IsNullOrEmpty(callback.TransactionRef))
                return ServiceResult.FailResult("Invalid transaction reference");

            // TODO: Verify signature from payment gateway

            var transaction = await _unitOfWork.Transactions
                .FirstOrDefaultAsync(t => t.ExternalRef == callback.TransactionRef);

            if (transaction == null)
                return ServiceResult.NotFoundResult("Transaction not found");

            if (transaction.Status != TransactionStatus.Pending)
                return ServiceResult.FailResult("Transaction has already been processed");

            var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId);
            if (wallet == null)
                return ServiceResult.FailResult("Wallet not found");

            if (callback.Status == "success")
            {
                transaction.Status = TransactionStatus.Success;
                wallet.Balance += transaction.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Wallets.Update(wallet);
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Send notification to user

                return ServiceResult.SuccessResult("Top up successful");
            }
            else
            {
                transaction.Status = TransactionStatus.Failed;
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.FailResult("Payment failed");
            }
        }

        public async Task<ServiceResult> WithdrawAsync(long userId, WithdrawRequestDto request)
        {
            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            
            if (wallet == null)
                return ServiceResult.NotFoundResult("Wallet not found");

            var availableBalance = wallet.Balance - wallet.LockedBalance;
            if (availableBalance < request.Amount)
                return ServiceResult.FailResult($"Insufficient available balance. Current balance: {availableBalance:N0} VND");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                // Lock the amount
                wallet.LockedBalance += request.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Wallets.Update(wallet);

                // Create pending withdrawal transaction
                var transactionRef = $"WD_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

                var transaction = new Transaction
                {
                    WalletId = wallet.Id,
                    Type = TransactionType.Withdraw,
                    Amount = request.Amount,
                    ExternalRef = transactionRef,
                    Status = TransactionStatus.Pending,
                    Description = $"Withdraw to {request.BankName} - {request.BankAccountNumber}",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Process withdrawal (manually or via API)
                // TODO: Send notification to admin and user

                return ServiceResult.SuccessResult("Withdrawal request submitted. Please allow 1-3 business days for processing.");
            });
        }

        public async Task<ServiceResult<PagedResponse<TransactionDto>>> GetTransactionsAsync(long userId, SearchTransactionDto searchDto)
        {
            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            
            if (wallet == null)
                return ServiceResult<PagedResponse<TransactionDto>>.NotFoundResult("Wallet not found");

            var (transactions, totalCount) = await _unitOfWork.Transactions.GetPagedByWalletAsync(
                wallet.Id, searchDto.PageNumber, searchDto.PageSize, searchDto.Type);

            var dtos = transactions.Select(MapToTransactionDto);
            var response = PagedResponse<TransactionDto>.Create(dtos, searchDto.PageNumber, searchDto.PageSize, totalCount);

            return ServiceResult<PagedResponse<TransactionDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<WalletSummaryDto>> GetWalletSummaryAsync(long userId)
        {
            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            
            if (wallet == null)
                return ServiceResult<WalletSummaryDto>.NotFoundResult("Wallet not found");

            var summary = await CalculateWalletSummary(wallet.Id);
            return ServiceResult<WalletSummaryDto>.SuccessResult(summary);
        }

        #region Private Methods

        private WalletDto MapToWalletDto(Wallet wallet)
        {
            return new WalletDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                LockedBalance = wallet.LockedBalance,
                Currency = wallet.Currency,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            };
        }

        private TransactionDto MapToTransactionDto(Transaction transaction)
        {
            return new TransactionDto
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                OrderId = transaction.OrderId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                ExternalRef = transaction.ExternalRef,
                Status = transaction.Status,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                OrderTitle = transaction.Order?.Request?.Title
            };
        }

        private async Task<WalletSummaryDto> CalculateWalletSummary(long walletId)
        {
            var transactions = await _unitOfWork.Transactions.GetByWalletIdAsync(walletId);
            var successTransactions = transactions.Where(t => t.Status == TransactionStatus.Success).ToList();

            return new WalletSummaryDto
            {
                TotalTopup = successTransactions.Where(t => t.Type == TransactionType.Topup).Sum(t => t.Amount),
                TotalWithdraw = successTransactions.Where(t => t.Type == TransactionType.Withdraw).Sum(t => t.Amount),
                TotalEarned = successTransactions.Where(t => t.Type == TransactionType.EscrowRelease).Sum(t => t.Amount),
                TotalSpent = successTransactions.Where(t => t.Type == TransactionType.EscrowHold).Sum(t => t.Amount),
                TotalTransactions = successTransactions.Count
            };
        }

        #endregion
    }
}
