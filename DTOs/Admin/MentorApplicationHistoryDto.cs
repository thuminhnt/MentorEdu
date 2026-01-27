using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Admin
{
    public class MentorApplicationHistoryDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserFullName { get; set; }
        public ApplicationAction Action { get; set; }
        public string ActionName => Action.ToString();
        public string? ApplicationSnapshot { get; set; }
        public string? CertificationUrls { get; set; }
        public string? Reason { get; set; }
        public decimal? AiConfidenceScore { get; set; }
        public string? AiModelVersion { get; set; }
        public int SubmissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? CreatedById { get; set; }
        public string? CreatedByEmail { get; set; }
    }

    public class MentorApplicationHistoryExportDto
    {
        public long UserId { get; set; }
        public int Action { get; set; }  // 0=Submitted, 1=Approved, 2=Rejected
        public string? ApplicationSnapshot { get; set; }
        public string? CertificationUrls { get; set; }
        public string? Reason { get; set; }
        public int SubmissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsManualReview { get; set; }  // true if reviewed by admin, false if auto
        public decimal? AiConfidenceScore { get; set; }
    }

    public class ApplicationHistoryQueryParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public long? UserId { get; set; }
        public ApplicationAction? Action { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
