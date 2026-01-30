using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("hashtags")]
    public class Hashtag
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        // Navigation Property
        public virtual ICollection<MentorHashtag> MentorHashtags { get; set; } = new List<MentorHashtag>();
        public virtual ICollection<CategoryHashtag> CategoryHashtags { get; set; } = new List<CategoryHashtag>();
        public virtual ICollection<CommunityPostHashtag> CommunityPostHashtags { get; set; } = new List<CommunityPostHashtag>();
    }
}
