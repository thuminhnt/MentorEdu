using System.ComponentModel.DataAnnotations;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Admin
{
    #region Requests Management

    /// <summary>Admin Request List DTO</summary>
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

    /// <summary>Admin Search Request DTO</summary>
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

    /// <summary>Admin Proposal List DTO</summary>
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

        // Thông tin mentor
        public long MentorId { get; set; }
        public string? MentorName { get; set; }
        public string? MentorEmail { get; set; }
        public string? MentorAvatar { get; set; }

        // Thông tin học viên (chủ yêu cầu)
        public long StudentId { get; set; }
        public string? StudentName { get; set; }
    }

    /// <summary>Admin Search Proposal DTO</summary>
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

    /// <summary>Admin Order List DTO</summary>
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

        // Học viên
        public long StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }

        // Mentor
        public long MentorId { get; set; }
        public string? MentorName { get; set; }
        public string? MentorEmail { get; set; }

        // Tóm tắt
        public bool HasEscrow { get; set; }
        public decimal? EscrowAmount { get; set; }
        public string? EscrowStatus { get; set; }
        public int ReportCount { get; set; }
    }

    /// <summary>Admin Search Order DTO</summary>
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

    /// <summary>Admin Update Order Status DTO</summary>
    public class AdminUpdateOrderStatusDto
    {
        [Required]
        public OrderStatus NewStatus { get; set; }

        public string? Note { get; set; }

        public bool NotifyUsers { get; set; } = true;
    }

    /// <summary>Admin Refund Order DTO</summary>
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
}
