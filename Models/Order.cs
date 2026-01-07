using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("request_id")]
        public long? RequestId { get; set; }

        [Column("proposal_id")]
        public long? ProposalId { get; set; }

        [Column("student_id")]
        public long StudentId { get; set; }

        [Column("mentor_id")]
        public long MentorId { get; set; }

        [Column("billing_type")]
        public BillingType BillingType { get; set; }

        [Column("agreed_price")]
        public decimal? AgreedPrice { get; set; }

        [Column("agreed_hourly_rate")]
        public decimal? AgreedHourlyRate { get; set; }

        [Column("total_price")]
        public decimal? TotalPrice { get; set; }

        [Column("status")]
        public OrderStatus Status { get; set; }

        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("cancelled_at")]
        public DateTime? CancelledAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("RequestId")]
        public virtual Request? Request { get; set; }

        [ForeignKey("ProposalId")]
        public virtual Proposal? Proposal { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; } = null!;

        [ForeignKey("MentorId")]
        public virtual User Mentor { get; set; } = null!;

        public virtual Escrow? Escrow { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<WorkSession> WorkSessions { get; set; } = new List<WorkSession>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
        public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    }
}
