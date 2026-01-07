using System.ComponentModel.DataAnnotations;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Wallet
{
    /// <summary>
    /// Wallet DTO
    /// </summary>
    public class WalletDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public decimal Balance { get; set; }
        public decimal LockedBalance { get; set; }
        public decimal AvailableBalance => Balance - LockedBalance;
        public string Currency { get; set; } = "VND";
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Format display
        public string BalanceDisplay => FormatCurrency(Balance);
        public string LockedBalanceDisplay => FormatCurrency(LockedBalance);
        public string AvailableBalanceDisplay => FormatCurrency(AvailableBalance);

        private string FormatCurrency(decimal amount)
        {
            return string.Format("{0:N0} {1}", amount, Currency);
        }
    }

    /// <summary>
    /// Wallet with recent transactions
    /// </summary>
    public class WalletDetailDto
    {
        public WalletDto? Wallet { get; set; }
        public List<TransactionDto>? RecentTransactions { get; set; }
        public WalletSummaryDto? Summary { get; set; }
    }

    /// <summary>
    /// Wallet Summary
    /// </summary>
    public class WalletSummaryDto
    {
        public decimal TotalTopup { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalTransactions { get; set; }
    }

    /// <summary>
    /// Transaction DTO
    /// </summary>
    public class TransactionDto
    {
        public long Id { get; set; }
        public long WalletId { get; set; }
        public long? OrderId { get; set; }
        public TransactionType Type { get; set; }
        public string TypeDisplay => GetTypeDisplay();
        public decimal Amount { get; set; }
        public string AmountDisplay => FormatAmount();
        public string? ExternalRef { get; set; }
        public TransactionStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Order info (if any)
        public string? OrderTitle { get; set; }

        private string GetTypeDisplay()
        {
            return Type switch
            {
                TransactionType.Topup => "Top Up",
                TransactionType.Withdraw => "Withdraw",
                TransactionType.EscrowHold => "Escrow Hold",
                TransactionType.EscrowRelease => "Payment Release",
                TransactionType.EscrowRefund => "Refund",
                TransactionType.ServiceFee => "Service Fee",
                TransactionType.Adjustment => "Adjustment",
                _ => Type.ToString()
            };
        }

        private string FormatAmount()
        {
            var sign = Type switch
            {
                TransactionType.Topup => "+",
                TransactionType.EscrowRelease => "+",
                TransactionType.EscrowRefund => "+",
                TransactionType.Withdraw => "-",
                TransactionType.EscrowHold => "-",
                TransactionType.ServiceFee => "-",
                _ => ""
            };
            return $"{sign}{Amount:N0} VND";
        }
    }

    /// <summary>
    /// Top Up Request
    /// </summary>
    public class TopupRequestDto
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(10000, 100000000, ErrorMessage = "Amount must be between 10,000 and 100,000,000 VND")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; } = string.Empty; // vnpay, momo, bank_transfer

        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// Top Up Response
    /// </summary>
    public class TopupResponseDto
    {
        public string? PaymentUrl { get; set; }
        public string? TransactionRef { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
    }

    /// <summary>
    /// Withdraw Request
    /// </summary>
    public class WithdrawRequestDto
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(50000, 50000000, ErrorMessage = "Withdrawal amount must be between 50,000 and 50,000,000 VND")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Bank account number is required")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bank name is required")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account holder name is required")]
        public string AccountHolderName { get; set; } = string.Empty;

        public string? Note { get; set; }
    }

    /// <summary>
    /// Search/Filter Transactions Request
    /// </summary>
    public class SearchTransactionDto
    {
        public TransactionType? Type { get; set; }
        public TransactionStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Payment Gateway Callback
    /// </summary>
    public class PaymentCallbackDto
    {
        public string? TransactionRef { get; set; }
        public string? Status { get; set; }
        public decimal? Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Signature { get; set; }
        public Dictionary<string, string>? ExtraData { get; set; }
    }
}
