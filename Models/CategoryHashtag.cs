using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    /// <summary>
    /// Many-to-many mapping between Category and Hashtag.
    /// Used to notify mentors with matching specializations when posts use related hashtags.
    /// </summary>
    [Table("category_hashtags")]
    public class CategoryHashtag
    {
        [Column("category_id")]
        public long CategoryId { get; set; }

        [Column("hashtag_id")]
        public long HashtagId { get; set; }

        // Navigation Properties
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        [ForeignKey("HashtagId")]
        public virtual Hashtag Hashtag { get; set; } = null!;
    }
}
