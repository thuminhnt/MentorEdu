using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    public enum TokenType
    {
        EmailVerification = 0,
        PasswordReset = 1
    }

    [Table("verification_tokens")]
    public class VerificationToken
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("user_id")]
        public long UserId { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Column("type")]
        public TokenType Type { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("is_used")]
        public bool IsUsed { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
