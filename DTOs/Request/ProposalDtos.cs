using System.ComponentModel.DataAnnotations;
using MentorEdu.DTOs.User;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Request
{
    /// <summary>
    /// Proposal List DTO
    /// </summary>
    public class ProposalListDto
    {
        public long Id { get; set; }
        public long RequestId { get; set; }
        public decimal? Price { get; set; }
        public string? Message { get; set; }
        public ProposalStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }

        // Mentor info
        public long MentorId { get; set; }
        public string? MentorName { get; set; }
        public string? MentorAvatar { get; set; }
        public string? MentorTitle { get; set; }
        public decimal? MentorRating { get; set; }
        public int? MentorRatingCount { get; set; }
    }

    /// <summary>
    /// Proposal Detail DTO
    /// </summary>
    public class ProposalDetailDto
    {
        public long Id { get; set; }
        public long RequestId { get; set; }
        public string? RequestTitle { get; set; }
        public decimal? Price { get; set; }
        public string? Message { get; set; }
        public ProposalStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Mentor detail
        public MentorListDto? Mentor { get; set; }

        // Student (request owner)
        public UserBasicDto? Student { get; set; }
    }

    /// <summary>
    /// Create Proposal Request
    /// </summary>
    public class CreateProposalDto
    {
        [Required(ErrorMessage = "Request ID is required")]
        public long RequestId { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update Proposal Request
    /// </summary>
    public class UpdateProposalDto
    {
        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Accept/Reject Proposal Request
    /// </summary>
    public class RespondProposalDto
    {
        [Required]
        public long ProposalId { get; set; }

        [Required]
        public bool Accept { get; set; }

        public string? Note { get; set; }
    }
}
