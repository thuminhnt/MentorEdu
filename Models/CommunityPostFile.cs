using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    /// <summary>
    /// File attachments for community posts (max 2 files per post)
    /// </summary>
    [Table("community_post_files")]
    public class CommunityPostFile
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("post_id")]
        public long PostId { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("file_url")]
        public string FileUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("file_name")]
        public string? FileName { get; set; }

        [MaxLength(50)]
        [Column("file_type")]
        public string FileType { get; set; } = "document"; // "image" or "document"

        [Column("file_size")]
        public long? FileSize { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("PostId")]
        public virtual CommunityPost Post { get; set; } = null!;
    }
}
