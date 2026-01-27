using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    /// <summary>
    /// Tracks all mentor application actions (submit, approve, reject)
    /// Used for audit trail and ML training data
    /// </summary>
    [Table("mentor_application_history")]
    public class MentorApplicationHistory
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// The user who submitted the application
        /// </summary>
        [Column("user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// Action performed (Submitted, Approved, Rejected, etc.)
        /// </summary>
        [Column("action")]
        public ApplicationAction Action { get; set; }

        /// <summary>
        /// JSON snapshot of application data at the time of action
        /// Contains: Specialization, ExperienceYears, Introduction, PortfolioUrl, Title, etc.
        /// </summary>
        [Column("application_snapshot")]
        public string? ApplicationSnapshot { get; set; }

        /// <summary>
        /// Certification URLs at the time of submission
        /// </summary>
        [Column("certification_urls")]
        public string? CertificationUrls { get; set; }

        /// <summary>
        /// Rejection reason (if rejected) or approval notes
        /// </summary>
        [Column("reason")]
        public string? Reason { get; set; }

        /// <summary>
        /// AI confidence score (0-100) if auto-reviewed
        /// Null for manual reviews
        /// </summary>
        [Column("ai_confidence_score")]
        public decimal? AiConfidenceScore { get; set; }

        /// <summary>
        /// AI model version used for auto-review
        /// </summary>
        [MaxLength(50)]
        [Column("ai_model_version")]
        public string? AiModelVersion { get; set; }

        /// <summary>
        /// When this action occurred
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who performed this action (Admin ID for approve/reject, User ID for submit)
        /// </summary>
        [Column("created_by")]
        public long? CreatedBy { get; set; }

        /// <summary>
        /// Submission count at the time of this action
        /// </summary>
        [Column("submission_count")]
        public int SubmissionCount { get; set; } = 1;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? ActionPerformedBy { get; set; }
    }
}
