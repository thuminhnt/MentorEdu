using System.ComponentModel.DataAnnotations;
using MentorEdu.DTOs.User;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Request
{
    /// <summary>
    /// Request Response DTO (for list display)
    /// </summary>
    public class RequestListDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? CategoryName { get; set; }
        public long? CategoryId { get; set; }
        public decimal? ExpectedBudget { get; set; }
        public decimal? ExpectedHours { get; set; }
        public RequestStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }
        public int ProposalCount { get; set; }

        // Student info
        public long StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentAvatar { get; set; }
    }

    /// <summary>
    /// Request Detail DTO
    /// </summary>
    public class RequestDetailDto
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal? ExpectedBudget { get; set; }
        public decimal? ExpectedHours { get; set; }
        public RequestStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Student info
        public UserBasicDto? Student { get; set; }

        // Proposals
        public List<ProposalListDto>? Proposals { get; set; }
        public int ProposalCount { get; set; }
    }

    /// <summary>
    /// Create Request DTO
    /// </summary>
    public class CreateRequestDto
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public long? CategoryId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Budget must be greater than 0")]
        public decimal? ExpectedBudget { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Hours must be greater than 0")]
        public decimal? ExpectedHours { get; set; }
    }

    /// <summary>
    /// Update Request DTO
    /// </summary>
    public class UpdateRequestDto
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public long? CategoryId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Budget must be greater than 0")]
        public decimal? ExpectedBudget { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Hours must be greater than 0")]
        public decimal? ExpectedHours { get; set; }
    }

    /// <summary>
    /// Search/Filter Request DTO
    /// </summary>
    public class SearchRequestDto
    {
        public string? Keyword { get; set; }
        public long? CategoryId { get; set; }
        public RequestStatus? Status { get; set; }
        public decimal? MinBudget { get; set; }
        public decimal? MaxBudget { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "createdAt";
        public bool SortDescending { get; set; } = true;
    }
}
