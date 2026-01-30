using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    /// <summary>
    /// Junction table for many-to-many relationship between CommunityPost and Hashtag
    /// Similar to MentorHashtag
    /// </summary>
    [Table("community_post_hashtags")]
    public class CommunityPostHashtag
    {
        [Column("post_id")]
        public long PostId { get; set; }

        [Column("hashtag_id")]
        public long HashtagId { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual CommunityPost Post { get; set; } = null!;

        [ForeignKey("HashtagId")]
        public virtual Hashtag Hashtag { get; set; } = null!;
    }
}
