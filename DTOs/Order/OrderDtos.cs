using System.ComponentModel.DataAnnotations;
using MentorEdu.DTOs.User;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Order
{
    /// <summary>
    /// Order List DTO
    /// </summary>
    public class OrderListDto
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
        public DateTime? CreatedAt { get; set; }

        // Student
        public long StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentAvatar { get; set; }

        // Mentor
        public long MentorId { get; set; }
        public string? MentorName { get; set; }
        public string? MentorAvatar { get; set; }

        // Summary
        public int TotalSessionMinutes { get; set; }
        public bool HasEscrow { get; set; }
    }

    /// <summary>
    /// Order Detail DTO
    /// </summary>
    public class OrderDetailDto
    {
        public long Id { get; set; }
        public long? RequestId { get; set; }
        public string? RequestTitle { get; set; }
        public long? ProposalId { get; set; }
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
        public DateTime? UpdatedAt { get; set; }

        // Student & Mentor
        public UserBasicDto? Student { get; set; }
        public MentorListDto? Mentor { get; set; }

        // Escrow
        public EscrowDto? Escrow { get; set; }

        // Work Sessions
        public List<WorkSessionListDto>? WorkSessions { get; set; }
        public int TotalSessionMinutes { get; set; }

        // Status History
        public List<OrderStatusHistoryDto>? StatusHistory { get; set; }
    }

    /// <summary>
    /// Order Status History DTO
    /// </summary>
    public class OrderStatusHistoryDto
    {
        public long Id { get; set; }
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
        public string? ChangedByName { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Escrow DTO
    /// </summary>
    public class EscrowDto
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public EscrowStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public DateTime? RefundedAt { get; set; }
    }

    /// <summary>
    /// Request t?o Order t? Proposal
    /// </summary>
    public class CreateOrderFromProposalDto
    {
        [Required]
        public long ProposalId { get; set; }

        [Required]
        public BillingType BillingType { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// Request t?o Order tr?c ti?p
    /// </summary>
    public class CreateDirectOrderDto
    {
        [Required]
        public long MentorId { get; set; }

        [Required]
        public BillingType BillingType { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? AgreedPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? AgreedHourlyRate { get; set; }

        public string? Description { get; set; }
    }

    /// <summary>
    /// Request c?p nh?t tr?ng thái Order
    /// </summary>
    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus NewStatus { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// Request filter Order
    /// </summary>
    public class SearchOrderDto
    {
        public OrderStatus? Status { get; set; }
        public BillingType? BillingType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "createdAt";
        public bool SortDescending { get; set; } = true;
    }
}
