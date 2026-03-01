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
        private readonly IMoMoPaymentService _momoService;
        private readonly IVNPayPaymentService _vnpayService;
        private readonly IVietQRService _vietqrService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WalletService(
            IUnitOfWork unitOfWork,
            IMoMoPaymentService momoService,
            IVNPayPaymentService vnpayService,
            IVietQRService vietqrService,
            IHttpContextAccessor httpContextAccessor) : base(unitOfWork)
        {
            _momoService = momoService;
            _vnpayService = vnpayService;
            _vietqrService = vietqrService;
            _httpContextAccessor = httpContextAccessor;
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

            var paymentMethod = request.PaymentMethod?.ToLower() ?? "momo";

            if (paymentMethod == "vnpay")
            {
                return await TopupViaVNPayAsync(userId, wallet!, request);
            }
            else if (paymentMethod == "bank_transfer")
            {
                return await TopupViaBankTransferAsync(userId, wallet!, request);
            }
            else
            {
                return await TopupViaMoMoAsync(userId, wallet!, request);
            }
        }

        private async Task<ServiceResult<TopupResponseDto>> TopupViaMoMoAsync(long userId, Wallet wallet, TopupRequestDto request)
        {
            // Create pending transaction
            var transactionRef = $"MOMO_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..50];

            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.Topup,
                Amount = request.Amount,
                ExternalRef = transactionRef,
                Status = TransactionStatus.Pending,
                Description = "Top up via MoMo",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            // Call MoMo API to create payment
            var orderInfo = $"MentorEdu wallet top up - {request.Amount:N0} VND";
            var momoResult = await _momoService.CreatePaymentAsync(
                transactionRef,
                (long)request.Amount,
                orderInfo,
                request.ReturnUrl);

            if (momoResult.ResultCode != 0 || string.IsNullOrEmpty(momoResult.PayUrl))
            {
                // MoMo API failed, mark transaction as failed
                transaction.Status = TransactionStatus.Failed;
                transaction.Description = $"MoMo error: {momoResult.Message}";
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult<TopupResponseDto>.FailResult(
                    $"Failed to create MoMo payment: {momoResult.Message}");
            }

            var response = new TopupResponseDto
            {
                PaymentUrl = momoResult.PayUrl,
                TransactionRef = transactionRef,
                Amount = request.Amount,
                PaymentMethod = "momo",
                ExpireAt = DateTime.UtcNow.AddMinutes(15)
            };

            return ServiceResult<TopupResponseDto>.SuccessResult(response, "Please complete the payment on MoMo");
        }

        private async Task<ServiceResult<TopupResponseDto>> TopupViaVNPayAsync(long userId, Wallet wallet, TopupRequestDto request)
        {
            // Create pending transaction
            var transactionRef = $"VNPAY_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..50];

            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.Topup,
                Amount = request.Amount,
                ExternalRef = transactionRef,
                Status = TransactionStatus.Pending,
                Description = "Top up via VNPay",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            // Get client IP
            var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (clientIp == "::1") clientIp = "127.0.0.1";

            var orderInfo = $"MentorEdu wallet top up - {request.Amount:N0} VND";

            try
            {
                var paymentUrl = _vnpayService.CreatePaymentUrl(
                    transactionRef,
                    (long)request.Amount,
                    orderInfo,
                    clientIp,
                    request.ReturnUrl);

                var response = new TopupResponseDto
                {
                    PaymentUrl = paymentUrl,
                    TransactionRef = transactionRef,
                    Amount = request.Amount,
                    PaymentMethod = "vnpay",
                    ExpireAt = DateTime.UtcNow.AddMinutes(15)
                };

                return ServiceResult<TopupResponseDto>.SuccessResult(response, "Please complete the payment on VNPay");
            }
            catch (Exception ex)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.Description = $"VNPay error: {ex.Message}";
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult<TopupResponseDto>.FailResult(
                    $"Failed to create VNPay payment: {ex.Message}");
            }
        }

        private async Task<ServiceResult<TopupResponseDto>> TopupViaBankTransferAsync(long userId, Wallet wallet, TopupRequestDto request)
        {
            // Create pending transaction with unique ref as transfer description
            var transactionRef = $"ME{userId}{DateTime.UtcNow:yyMMddHHmmss}";

            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.Topup,
                Amount = request.Amount,
                ExternalRef = transactionRef,
                Status = TransactionStatus.Pending,
                Description = "Top up via bank transfer (VietQR)",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            // Generate VietQR code
            var transferDesc = $"MENTOREDU {transactionRef}";
            var qrResult = await _vietqrService.GenerateQRAsync(request.Amount, transferDesc);

            if (!qrResult.Success)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.Description = $"VietQR error: {qrResult.Message}";
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult<TopupResponseDto>.FailResult(
                    $"Failed to generate QR code: {qrResult.Message}");
            }

            var response = new TopupResponseDto
            {
                TransactionRef = transactionRef,
                Amount = request.Amount,
                PaymentMethod = "bank_transfer",
                ExpireAt = DateTime.UtcNow.AddHours(24),
                QrCodeUrl = qrResult.QrDataUrl,
                BankId = qrResult.BankId,
                AccountNo = qrResult.AccountNo,
                AccountName = qrResult.AccountName,
                TransferDescription = transferDesc,
            };

            return ServiceResult<TopupResponseDto>.SuccessResult(response, "Please transfer using the information below");
        }

        public async Task<ServiceResult> ConfirmBankTransferAsync(long adminUserId, string transactionRef)
        {
            var transaction = await _unitOfWork.Transactions
                .FirstOrDefaultAsync(t => t.ExternalRef == transactionRef);

            if (transaction == null)
                return ServiceResult.NotFoundResult("Transaction not found");

            if (transaction.Status != TransactionStatus.Pending)
                return ServiceResult.FailResult("Transaction has already been processed");

            var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId);
            if (wallet == null)
                return ServiceResult.FailResult("Wallet not found");

            transaction.Status = TransactionStatus.Success;
            transaction.Description = $"Bank transfer top up - Confirmed by admin (ID: {adminUserId})";

            wallet.Balance += transaction.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Wallets.Update(wallet);
            _unitOfWork.Transactions.Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Bank transfer confirmed successfully");
        }

        public async Task<ServiceResult> RejectBankTransferAsync(long adminUserId, string transactionRef)
        {
            var transaction = await _unitOfWork.Transactions
                .FirstOrDefaultAsync(t => t.ExternalRef == transactionRef);

            if (transaction == null)
                return ServiceResult.NotFoundResult("Transaction not found");

            if (transaction.Status != TransactionStatus.Pending)
                return ServiceResult.FailResult("Transaction has already been processed");

            transaction.Status = TransactionStatus.Failed;
            transaction.Description = $"Bank transfer rejected by admin (ID: {adminUserId})";

            _unitOfWork.Transactions.Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Bank transfer rejected successfully");
        }

        public async Task<ServiceResult<PagedResponse<TransactionDto>>> GetPendingBankTransfersAsync(int pageNumber = 1, int pageSize = 20)
        {
            var query = _unitOfWork.Transactions.Query()
                .Where(t => t.Type == TransactionType.Topup
                    && t.Status == TransactionStatus.Pending
                    && t.Description != null
                    && t.Description.Contains("VietQR"))
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await Task.Run(() => query.Count());
            var items = await Task.Run(() => query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList());

            // Get wallet -> user info
            var walletIds = items.Select(t => t.WalletId).Distinct().ToList();
            var wallets = await Task.Run(() => _unitOfWork.Wallets.Query()
                .Where(w => walletIds.Contains(w.Id))
                .ToList());

            var userIds = wallets.Select(w => w.UserId).Distinct().ToList();
            var users = await Task.Run(() => _unitOfWork.Users.Query()
                .Where(u => userIds.Contains(u.Id))
                .ToList());

            var walletUserMap = wallets.ToDictionary(w => w.Id, w => users.FirstOrDefault(u => u.Id == w.UserId));

            var dtos = items.Select(t =>
            {
                var user = walletUserMap.GetValueOrDefault(t.WalletId);
                return new TransactionDto
                {
                    Id = t.Id,
                    WalletId = t.WalletId,
                    OrderId = t.OrderId,
                    Type = t.Type,
                    Amount = t.Amount,
                    ExternalRef = t.ExternalRef,
                    Status = t.Status,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    OrderTitle = user != null ? $"{user.Email}" : null,
                };
            }).ToList();

            var result = new PagedResponse<TransactionDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            return ServiceResult<PagedResponse<TransactionDto>>.SuccessResult(result);
        }

        public async Task<ServiceResult> ProcessTopupCallbackAsync(PaymentCallbackDto callback)
        {
            if (string.IsNullOrEmpty(callback.TransactionRef))
                return ServiceResult.FailResult("Invalid transaction reference");

            var transaction = await _unitOfWork.Transactions
                .FirstOrDefaultAsync(t => t.ExternalRef == callback.TransactionRef);

            if (transaction == null)
                return ServiceResult.NotFoundResult("Transaction not found");

            if (transaction.Status != TransactionStatus.Pending)
                return ServiceResult.FailResult("Transaction has already been processed");

            var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId);
            if (wallet == null)
                return ServiceResult.FailResult("Wallet not found");

            if (callback.Status == "success" || callback.Status == "0")
            {
                transaction.Status = TransactionStatus.Success;
                wallet.Balance += transaction.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Wallets.Update(wallet);
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("Top-up successful");
            }
            else
            {
                transaction.Status = TransactionStatus.Failed;
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.FailResult("Payment failed");
            }
        }

        public async Task<ServiceResult> ProcessMoMoIpnAsync(Dictionary<string, object> ipnData)
        {
            // Extract data from MoMo IPN
            var orderId = ipnData.TryGetValue("orderId", out var oid) ? oid?.ToString() : null;
            var resultCodeStr = ipnData.TryGetValue("resultCode", out var rc) ? rc?.ToString() : null;
            var signature = ipnData.TryGetValue("signature", out var sig) ? sig?.ToString() : null;

            if (string.IsNullOrEmpty(orderId))
                return ServiceResult.FailResult("Missing orderId");

            // Validate signature
            if (!string.IsNullOrEmpty(signature))
            {
                var callbackDict = new Dictionary<string, string>();
                foreach (var kvp in ipnData)
                {
                    if (kvp.Key != "signature")
                        callbackDict[kvp.Key] = kvp.Value?.ToString() ?? "";
                }

                var isValid = _momoService.ValidateCallback(callbackDict, signature);
                if (!isValid)
                {
                    return ServiceResult.FailResult("Invalid signature");
                }
            }

            // Find transaction by orderId (which is our transactionRef)
            var transaction = await _unitOfWork.Transactions
                .FirstOrDefaultAsync(t => t.ExternalRef == orderId);

            if (transaction == null)
                return ServiceResult.NotFoundResult("Transaction not found");

            if (transaction.Status != TransactionStatus.Pending)
                return ServiceResult.SuccessResult("Transaction already processed");

            var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId);
            if (wallet == null)
                return ServiceResult.FailResult("Wallet not found");

            // resultCode = 0 means success
            if (resultCodeStr == "0")
            {
                transaction.Status = TransactionStatus.Success;
                var momoTransId = ipnData.TryGetValue("transId", out var tid) ? tid?.ToString() : null;
                if (!string.IsNullOrEmpty(momoTransId))
                {
                    transaction.Description = $"Top-up via MoMo (MoMo TransId: {momoTransId})";
                }

                wallet.Balance += transaction.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Wallets.Update(wallet);
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("Top-up successful");
            }
            else
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.Description = $"MoMo payment failed (resultCode: {resultCodeStr})";
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.FailResult("Payment failed");
            }
        }

        public async Task<ServiceResult> ProcessVNPayCallbackAsync(IQueryCollection queryParams)
        {
            // Validate signature
            var isValid = _vnpayService.ValidateCallback(queryParams);
            if (!isValid)
            {
                return ServiceResult.FailResult("Invalid VNPay signature");
            }

            var data = _vnpayService.ParseCallbackData(queryParams);

            var txnRef = data.GetValueOrDefault("vnp_TxnRef", "");
            var responseCode = data.GetValueOrDefault("vnp_ResponseCode", "");
            var transactionNo = data.GetValueOrDefault("vnp_TransactionNo", "");
            var vnpAmount = data.GetValueOrDefault("vnp_Amount", "0");

            if (string.IsNullOrEmpty(txnRef))
                return ServiceResult.FailResult("Missing vnp_TxnRef");

            // Find transaction by reference
            var transaction = await _unitOfWork.Transactions
                .FirstOrDefaultAsync(t => t.ExternalRef == txnRef);

            if (transaction == null)
                return ServiceResult.NotFoundResult("Transaction not found");

            if (transaction.Status != TransactionStatus.Pending)
                return ServiceResult.SuccessResult("Transaction already processed");

            var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId);
            if (wallet == null)
                return ServiceResult.FailResult("Wallet not found");

            // VNPay response code "00" means success
            if (responseCode == "00")
            {
                transaction.Status = TransactionStatus.Success;
                if (!string.IsNullOrEmpty(transactionNo))
                {
                    transaction.Description = $"Top-up via VNPay (VNPay TransNo: {transactionNo})";
                }

                wallet.Balance += transaction.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Wallets.Update(wallet);
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("Top-up successful");
            }
            else
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.Description = $"VNPay payment failed (responseCode: {responseCode})";
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.FailResult($"Payment failed with code: {responseCode}");
            }
        }

        public async Task<ServiceResult<TransactionDto>> CheckTransactionStatusAsync(string transactionRef)
        {
            var transaction = await _unitOfWork.Transactions
                .FirstOrDefaultAsync(t => t.ExternalRef == transactionRef);

            if (transaction == null)
                return ServiceResult<TransactionDto>.NotFoundResult("Transaction not found");

            var dto = MapToTransactionDto(transaction);
            return ServiceResult<TransactionDto>.SuccessResult(dto);
        }

        public async Task<ServiceResult> WithdrawAsync(long userId, WithdrawRequestDto request)
        {
            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            
            if (wallet == null)
                return ServiceResult.NotFoundResult("Wallet not found");

            var availableBalance = wallet.Balance - wallet.LockedBalance;
            if (availableBalance < request.Amount)
                return ServiceResult.FailResult($"Insufficient balance. Available: {availableBalance:N0} VND");

            return await _unitOfWork.ExecuteWithStrategyAsync(async () =>
            {
                // Deduct balance immediately
                wallet.Balance -= request.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Wallets.Update(wallet);

                // Create completed withdrawal transaction
                var transactionRef = $"WD_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

                var transaction = new Transaction
                {
                    WalletId = wallet.Id,
                    Type = TransactionType.Withdraw,
                    Amount = request.Amount,
                    ExternalRef = transactionRef,
                    Status = TransactionStatus.Success,
                    Description = $"Withdraw to {request.BankName} - {request.BankAccountNumber} ({request.AccountHolderName})",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.SuccessResult("Withdrawal successful. Funds have been sent to your bank account.");
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
