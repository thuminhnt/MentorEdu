using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("reports")]
    public class Report
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("reporter_id")]
        public long ReporterId { get; set; }

        [Column("reported_user")]
        public long ReportedUserId { get; set; }

        [Column("order_id")]
        public long? OrderId { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [MaxLength(50)]
        [Column("status")]
        public string? Status { get; set; }

        [Column("handled_by")]
        public long? HandledBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("ReporterId")]
        public virtual User Reporter { get; set; } = null!;

        [ForeignKey("ReportedUserId")]
        public virtual User ReportedUser { get; set; } = null!;

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("HandledBy")]
        public virtual User? Handler { get; set; }
    }
}
