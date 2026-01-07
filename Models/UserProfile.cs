using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("user_profiles")]
    public class UserProfile
    {
        [Key]
        [Column("user_id")]
        public long UserId { get; set; }

        [MaxLength(255)]
        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [MaxLength(30)]
        [Column("phone")]
        public string? Phone { get; set; }

        [MaxLength(20)]
        [Column("gender")]
        public string? Gender { get; set; }

        [MaxLength(255)]
        [Column("school")]
        public string? School { get; set; }

        [MaxLength(255)]
        [Column("major")]
        public string? Major { get; set; }

        [Column("bio")]
        public string? Bio { get; set; }

        [MaxLength(100)]
        [Column("city")]
        public string? City { get; set; }

        [MaxLength(100)]
        [Column("country")]
        public string? Country { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
