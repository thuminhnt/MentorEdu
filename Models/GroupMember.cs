using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("group_members")]
    public class GroupMember
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("group_id")]
        public long GroupId { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Column("role")]
        public GroupRole Role { get; set; } = GroupRole.Member; // Admin (Mentor) or Member (Student)

        [ForeignKey("GroupId")]
        public virtual Group Group { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    public enum GroupRole
    {
        Member = 0,
        Admin = 1
    }
}
