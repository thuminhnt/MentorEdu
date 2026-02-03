using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("group_messages")]
    public class GroupMessage
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("group_id")]
        public long GroupId { get; set; }

        [Column("sender_id")]
        public long SenderId { get; set; }

        [Column("content")]
        public string? Content { get; set; }

        [Column("message_type")]
        public MessageType MessageType { get; set; } = MessageType.Text;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("GroupId")]
        public virtual Group Group { get; set; } = null!;

        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; } = null!;

        // Reuse existing MessageFile logic if needed, or create GroupMessageFile
        // For simplicity, let's assume files are handled similarly or add a navigation property if needed later.
        // For now, simple text messages or we can reuse MessageFile by making it polymorphic or adding GroupMessageId.
        // Let's create a specific collection for group message files if needed, but for MVP let's start with basic messages.
    }
}
