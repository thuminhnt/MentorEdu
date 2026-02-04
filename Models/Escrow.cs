using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("escrows")]
    public class Escrow
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("order_id")]
        public long? OrderId { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("status")]
        public EscrowStatus Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("released_at")]
        public DateTime? ReleasedAt { get; set; }

        [Column("refunded_at")]
        public DateTime? RefundedAt { get; set; }

        // Navigation Property
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}
