using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MentorEdu.Models.Enums;

namespace MentorEdu.Models
{
    [Table("messages")]
    public class Message
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("conversation_id")]
        public long ConversationId { get; set; }

        [Column("sender_id")]
        public long SenderId { get; set; }

        [Column("content")]
        public string? Content { get; set; }

        [Column("message_type")]
        public MessageType MessageType { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;

        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; } = null!;

        public virtual ICollection<MessageFile> MessageFiles { get; set; } = new List<MessageFile>();
    }
}
