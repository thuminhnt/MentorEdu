using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("community_comments")]
    public class CommunityComment
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("post_id")]
        public long PostId { get; set; }

        [Column("author_id")]
        public long AuthorId { get; set; }

        [Column("content")]
        public string? Content { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual CommunityPost Post { get; set; } = null!;

        [ForeignKey("AuthorId")]
        public virtual User Author { get; set; } = null!;
    }
}
