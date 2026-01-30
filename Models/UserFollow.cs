using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("user_follows")]
    public class UserFollow
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("follower_id")]
        public long FollowerId { get; set; }

        [Column("following_id")]
        public long FollowingId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("FollowerId")]
        public virtual User Follower { get; set; } = null!;

        [ForeignKey("FollowingId")]
        public virtual User Following { get; set; } = null!;
    }
}
