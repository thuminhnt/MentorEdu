using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("reviews")]
    public class Review
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("order_id")]
        public long OrderId { get; set; }

        [Column("from_user")]
        public long FromUserId { get; set; }

        [Column("to_user")]
        public long ToUserId { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("FromUserId")]
        public virtual User FromUser { get; set; } = null!;

        [ForeignKey("ToUserId")]
        public virtual User ToUser { get; set; } = null!;
    }
}
