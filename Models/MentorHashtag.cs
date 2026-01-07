using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("mentor_hashtags")]
    public class MentorHashtag
    {
        [Column("mentor_id")]
        public long MentorId { get; set; }

        [Column("hashtag_id")]
        public long HashtagId { get; set; }

        // Navigation Properties
        [ForeignKey("MentorId")]
        public virtual User Mentor { get; set; } = null!;

        [ForeignKey("HashtagId")]
        public virtual Hashtag Hashtag { get; set; } = null!;
    }
}
