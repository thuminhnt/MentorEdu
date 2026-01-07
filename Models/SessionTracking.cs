using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("session_tracking")]
    public class SessionTracking
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("session_id")]
        public long SessionId { get; set; }

        [Column("start_at")]
        public DateTime? StartAt { get; set; }

        [Column("end_at")]
        public DateTime? EndAt { get; set; }

        [Column("duration_min")]
        public int? DurationMin { get; set; }

        // Navigation Property
        [ForeignKey("SessionId")]
        public virtual WorkSession WorkSession { get; set; } = null!;
    }
}
