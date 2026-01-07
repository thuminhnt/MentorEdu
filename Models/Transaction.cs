using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("wallet_id")]
        public long WalletId { get; set; }

        [Column("order_id")]
        public long? OrderId { get; set; }

        [Column("type")]
        public TransactionType Type { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        [Column("external_ref")]
        public string? ExternalRef { get; set; }

        [Column("status")]
        public TransactionStatus Status { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("WalletId")]
        public virtual Wallet Wallet { get; set; } = null!;

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}
