using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.User
{
    /// <summary>
    /// DTO for mentor application status (shown in profile page)
    /// </summary>
    public class MentorApplicationStatusDto
    {
        public bool HasApplied { get; set; }
        public MentorApprovalStatus? Status { get; set; }
        public string? StatusText { get; set; }
        public DateTime? AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        
        // Application details (if exists)
        public string? Specialization { get; set; }
        public int? ExperienceYears { get; set; }
    }
}
