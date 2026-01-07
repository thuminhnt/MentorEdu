using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    [Table("message_files")]
    public class MessageFile
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("message_id")]
        public long MessageId { get; set; }

        [Column("file_url")]
        public string? FileUrl { get; set; }

        [MaxLength(50)]
        [Column("file_type")]
        public string? FileType { get; set; }

        [MaxLength(255)]
        [Column("file_name")]
        public string? FileName { get; set; }

        [Column("file_size")]
        public long? FileSize { get; set; }

        // Navigation Property
        [ForeignKey("MessageId")]
        public virtual Message Message { get; set; } = null!;
    }
}
