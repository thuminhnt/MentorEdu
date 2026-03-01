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

        [Column("category_id")]
        public long? CategoryId { get; set; }

        [MaxLength(255)]
        [Column("title")]
        public string? Title { get; set; }

        [Column("content")]
        public string? Content { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("is_hidden")]
        public bool IsHidden { get; set; } = false;

        [MaxLength(500)]
        [Column("hidden_reason")]
        public string? HiddenReason { get; set; }

        // Navigation Properties
        [ForeignKey("AuthorId")]
        public virtual User Author { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
        public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
        public virtual ICollection<CommunityPostHashtag> PostHashtags { get; set; } = new List<CommunityPostHashtag>();
        public virtual ICollection<CommunityPostFile> Files { get; set; } = new List<CommunityPostFile>();
    }
}
