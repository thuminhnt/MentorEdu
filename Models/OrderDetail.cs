using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("order_details")]
    public class OrderDetail
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("order_id")]
        public long OrderId { get; set; }

        [MaxLength(20)]
        [Column("old_status")]
        public string? OldStatus { get; set; }

        [MaxLength(20)]
        [Column("new_status")]
        public string? NewStatus { get; set; }

        [Column("changed_by")]
        public long? ChangedBy { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("ChangedBy")]
        public virtual User? ChangedByUser { get; set; }
    }
}
