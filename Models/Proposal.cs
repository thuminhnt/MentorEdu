using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("proposals")]
    public class Proposal
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("request_id")]
        public long? RequestId { get; set; }

        [Column("post_id")]
        public long? PostId { get; set; }

        [Column("mentor_id")]
        public long MentorId { get; set; }

        [Column("price")]
        public decimal? Price { get; set; }

        [Column("message")]
        public string? Message { get; set; }

        [Column("status")]
        public ProposalStatus Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("RequestId")]
        public virtual Request? Request { get; set; }

        [ForeignKey("PostId")]
        public virtual CommunityPost? Post { get; set; }

        [ForeignKey("MentorId")]
        public virtual User Mentor { get; set; } = null!;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
