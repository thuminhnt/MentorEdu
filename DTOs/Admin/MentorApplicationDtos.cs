using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Admin
{
    /// <summary>
    /// Mentor Application DTO for admin view
    /// </summary>
    public class MentorApplicationDto
    {
        public long UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        
        // Application details
        public string? Specialization { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Introduction { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? CertificationUrls { get; set; }
        public string? ApplicationNote { get; set; }
        
        // Status
        public MentorApprovalStatus ApprovalStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedByName { get; set; }
        public string? RejectionReason { get; set; }
    }

    /// <summary>
    /// Request to reject a mentor application
    /// </summary>
    public class RejectMentorRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
