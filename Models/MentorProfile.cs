using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
