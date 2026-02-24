using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorEdu.Models
{
    public class AIChat
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// UserId of the user who sent/received the message. Null for guest users.
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// Session ID to group messages in a conversation
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Role: "user" or "assistant"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Message content
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the message was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
