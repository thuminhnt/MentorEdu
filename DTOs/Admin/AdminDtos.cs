using System.ComponentModel.DataAnnotations;
using MentorEdu.DTOs.User;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Admin
{
    #region Requests Management

    /// <summary>
    /// Admin Request List DTO
    /// </summary>
    public class AdminRequestListDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public long? CategoryId { get; set; }
        public decimal? ExpectedBudget { get; set; }
        public RequestStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }
        public int ProposalCount { get; set; }
        public int OrderCount { get; set; }

        // Student info
        public long StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
        public string? StudentAvatar { get; set; }
    }

    /// <summary>
    /// Admin Search Request DTO
    /// </summary>
    public class AdminSearchRequestDto
    {
        public string? Keyword { get; set; }
        public long? CategoryId { get; set; }
        public RequestStatus? Status { get; set; }
        public long? StudentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    #endregion

    #region Proposals Management

    /// <summary>
    /// Admin Proposal List DTO
    /// </summary>
    public class AdminProposalListDto
    {
        public long Id { get; set; }
        public long RequestId { get; set; }
        public string? RequestTitle { get; set; }
        public decimal? Price { get; set; }
        public string? Message { get; set; }
        public ProposalStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }

        // Mentor info
        public long MentorId { get; set; }
        public string? MentorName { get; set; }
        public string? MentorEmail { get; set; }
        public string? MentorAvatar { get; set; }

        // Student info (request owner)
        public long StudentId { get; set; }
        public string? StudentName { get; set; }
    }

    /// <summary>
    /// Admin Search Proposal DTO
    /// </summary>
    public class AdminSearchProposalDto
    {
        public long? RequestId { get; set; }
        public long? MentorId { get; set; }
        public ProposalStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    #endregion

    #region Orders Management

    /// <summary>
    /// Admin Order List DTO
    /// </summary>
    public class AdminOrderListDto
    {
        public long Id { get; set; }
        public string? RequestTitle { get; set; }
        public BillingType BillingType { get; set; }
        public string BillingTypeDisplay => BillingType.ToString();
        public decimal? AgreedPrice { get; set; }
        public decimal? AgreedHourlyRate { get; set; }
        public decimal? TotalPrice { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Student
        public long StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }

        // Mentor
        public long MentorId { get; set; }
        public string? MentorName { get; set; }
        public string? MentorEmail { get; set; }

        // Summary
        public bool HasEscrow { get; set; }
        public decimal? EscrowAmount { get; set; }
        public string? EscrowStatus { get; set; }
        public int ReportCount { get; set; }
    }

    /// <summary>
    /// Admin Search Order DTO
    /// </summary>
    public class AdminSearchOrderDto
    {
        public string? Keyword { get; set; }
        public OrderStatus? Status { get; set; }
        public BillingType? BillingType { get; set; }
        public long? StudentId { get; set; }
        public long? MentorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? HasDispute { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Admin Update Order Status DTO
    /// </summary>
    public class AdminUpdateOrderStatusDto
    {
        [Required]
        public OrderStatus NewStatus { get; set; }

        public string? Note { get; set; }

        public bool NotifyUsers { get; set; } = true;
    }

    /// <summary>
    /// Admin Refund Order DTO
    /// </summary>
    public class AdminRefundOrderDto
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public bool FullRefund { get; set; } = true;
    }

    #endregion

    #region Community Management

    /// <summary>
    /// Admin Post List DTO
    /// </summary>
    public class AdminPostListDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? ContentPreview { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Author
        public long AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
        public string? AuthorAvatar { get; set; }

        // Stats
        public int CommentCount { get; set; }
        public int ReportCount { get; set; }
        public bool IsHidden { get; set; }
    }

    /// <summary>
    /// Admin Search Post DTO
    /// </summary>
    public class AdminSearchPostDto
    {
        public string? Keyword { get; set; }
        public long? AuthorId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsHidden { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Admin Comment List DTO
    /// </summary>
    public class AdminCommentListDto
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public string? PostTitle { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Author
        public long AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorEmail { get; set; }
    }

    /// <summary>
    /// Admin Search Comment DTO
    /// </summary>
    public class AdminSearchCommentDto
    {
        public long? PostId { get; set; }
        public long? AuthorId { get; set; }
        public string? Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    #endregion

    #region Transactions Management

    /// <summary>
    /// Admin Transaction List DTO
    /// </summary>
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

    /// <summary>
    /// Admin Search Transaction DTO
    /// </summary>
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

    /// <summary>
    /// Admin Wallet List DTO
    /// </summary>
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

    /// <summary>
    /// Admin Search Wallet DTO
    /// </summary>
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

    #region Reviews Management

    /// <summary>
    /// Admin Review List DTO
    /// </summary>
    public class AdminReviewListDto
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string? OrderTitle { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }

        // From User
        public long FromUserId { get; set; }
        public string? FromUserName { get; set; }
        public string? FromUserEmail { get; set; }

        // To User
        public long ToUserId { get; set; }
        public string? ToUserName { get; set; }
        public string? ToUserEmail { get; set; }
    }

    /// <summary>
    /// Admin Search Review DTO
    /// </summary>
    public class AdminSearchReviewDto
    {
        public long? OrderId { get; set; }
        public long? FromUserId { get; set; }
        public long? ToUserId { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    #endregion

    #region Hashtags Management

    /// <summary>
    /// Admin Hashtag DTO with usage count
    /// </summary>
    public class AdminHashtagDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MentorCount { get; set; }
        public int CategoryCount { get; set; }
    }

    /// <summary>
    /// Create Hashtag DTO
    /// </summary>
    public class CreateHashtagDto
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Admin Search Hashtag DTO
    /// </summary>
    public class AdminSearchHashtagDto
    {
        public string? Keyword { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    #endregion

    #region User Management

    /// <summary>
    /// Admin Create User DTO
    /// </summary>
    public class AdminCreateUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public UserRole Role { get; set; }

        [Phone(ErrorMessage = "Invalid phone format")]
        public string? Phone { get; set; }

        public string? Gender { get; set; }
        public string? School { get; set; }
        public string? Major { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        public bool IsVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Admin Update User DTO
    /// </summary>
    public class AdminUpdateUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        // ? Add password field (optional - only update if provided)
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [MaxLength(100, ErrorMessage = "Password must be less than 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string? Password { get; set; }

        [Phone(ErrorMessage = "Invalid phone format")]
        public string? Phone { get; set; }

        public string? Gender { get; set; }
        public string? School { get; set; }
        public string? Major { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}
