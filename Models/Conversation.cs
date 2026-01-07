using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("conversations")]
    public class Conversation
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("request_id")]
        public long? RequestId { get; set; }

        [Column("order_id")]
        public long? OrderId { get; set; }

        [Column("student_id")]
        public long StudentId { get; set; }

        [Column("mentor_id")]
        public long MentorId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("RequestId")]
        public virtual Request? Request { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; } = null!;

        [ForeignKey("MentorId")]
        public virtual User Mentor { get; set; } = null!;

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
