using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("mentor_profiles")]
    public class MentorProfile
    {
        [Key]
        [Column("user_id")]
        public long UserId { get; set; }

        [MaxLength(255)]
        [Column("title")]
        public string? Title { get; set; }

        [Column("hourly_rate")]
        public decimal? HourlyRate { get; set; }

        [Column("package_price")]
        public decimal? PackagePrice { get; set; }

        [Column("experience_years")]
        public int? ExperienceYears { get; set; }

        [Column("introduction")]
        public string? Introduction { get; set; }

        [Column("availability_note")]
        public string? AvailabilityNote { get; set; }

        [Column("rating_avg")]
        public decimal? RatingAvg { get; set; }

        [Column("rating_count")]
        public int? RatingCount { get; set; }

        // ========== Mentor Application Fields ==========
        
        /// <summary>
        /// Approval status for mentor application
        /// </summary>
        [Column("approval_status")]
        public MentorApprovalStatus ApprovalStatus { get; set; } = MentorApprovalStatus.Pending;

        /// <summary>
        /// Mentor's specialization/expertise area
        /// </summary>
        [MaxLength(500)]
        [Column("specialization")]
        public string? Specialization { get; set; }

        /// <summary>
        /// Portfolio URL (GitHub, LinkedIn, personal website)
        /// </summary>
        [MaxLength(500)]
        [Column("portfolio_url")]
        public string? PortfolioUrl { get; set; }

        /// <summary>
        /// Comma-separated URLs of uploaded certification files
        /// Example: "/uploads/certifications/abc.pdf,/uploads/certifications/xyz.jpg"
        /// </summary>
        [Column("certification_urls")]
        public string? CertificationUrls { get; set; }

        /// <summary>
        /// Additional notes from applicant
        /// </summary>
        [Column("application_note")]
        public string? ApplicationNote { get; set; }

        /// <summary>
        /// When the application was reviewed
        /// </summary>
        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// Admin user ID who reviewed the application
        /// </summary>
        [Column("reviewed_by")]
        public long? ReviewedBy { get; set; }

        /// <summary>
        /// Reason for rejection (if rejected)
        /// </summary>
        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        // ========== Timestamps ==========

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ReviewedBy")]
        public virtual User? Reviewer { get; set; }
    }
}

