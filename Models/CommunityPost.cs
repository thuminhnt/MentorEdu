using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("community_posts")]
    public class CommunityPost
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("author_id")]
        public long AuthorId { get; set; }

        [MaxLength(255)]
        [Column("title")]
        public string? Title { get; set; }

        [Column("content")]
        public string? Content { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("AuthorId")]
        public virtual User Author { get; set; } = null!;

        public virtual ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
    }
}
