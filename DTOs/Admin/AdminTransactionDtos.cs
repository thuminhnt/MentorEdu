using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Admin
{
    #region Transactions Management

    /// <summary>Admin Transaction List DTO</summary>
    public class AdminTransactionListDto
    {
        public long Id { get; set; }
        public long WalletId { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public long? OrderId { get; set; }
        public TransactionType Type { get; set; }
        public string TypeDisplay => Type.ToString();
        public decimal Amount { get; set; }
        public string? ExternalRef { get; set; }
        public TransactionStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>Admin Search Transaction DTO</summary>
    public class AdminSearchTransactionDto
    {
        public long? UserId { get; set; }
        public TransactionType? Type { get; set; }
        public TransactionStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>Admin Wallet List DTO</summary>
    public class AdminWalletListDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public decimal Balance { get; set; }
        public decimal LockedBalance { get; set; }
        public decimal AvailableBalance => Balance - LockedBalance;
        public string Currency { get; set; } = "VND";
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TransactionCount { get; set; }
    }

    /// <summary>Admin Search Wallet DTO</summary>
    public class AdminSearchWalletDto
    {
        public long? UserId { get; set; }
        public string? UserEmail { get; set; }
        public decimal? MinBalance { get; set; }
        public decimal? MaxBalance { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    #endregion
}
