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
        
        // Categories & Hashtags
        public List<CategorySimpleDto> Categories { get; set; } = new();
        public List<HashtagSimpleDto> Hashtags { get; set; } = new();
        
        // Status
        public MentorApprovalStatus ApprovalStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedByName { get; set; }
        public string? RejectionReason { get; set; }

        // AI Analysis fields
        public decimal? AiConfidenceScore { get; set; }
        public string? AiRecommendedAction { get; set; }
        public string? AiReasoning { get; set; }
        public List<string>? AiKeyPoints { get; set; }
        public List<string>? AiRedFlags { get; set; }
        public string? AiModelVersion { get; set; }
        public bool IsAiReviewed { get; set; }
    }
    
    public class CategorySimpleDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    public class HashtagSimpleDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to reject a mentor application
    /// </summary>
    public class RejectMentorRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
