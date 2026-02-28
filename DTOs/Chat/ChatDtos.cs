using System.ComponentModel.DataAnnotations;
using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Chat
{
    /// <summary>
    /// Conversation List DTO
    /// </summary>
    public class ConversationListDto
    {
        public long Id { get; set; }
        public long? RequestId { get; set; }
        public string? RequestTitle { get; set; }
        public long? OrderId { get; set; }
        public string? OrderStatus { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Other participant
        public long ParticipantId { get; set; }
        public string? ParticipantName { get; set; }
        public string? ParticipantAvatar { get; set; }
        public bool IsParticipantMentor { get; set; }

        // Last message
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool LastMessageIsRead { get; set; }
        public int UnreadCount { get; set; }
    }

    /// <summary>
    /// Conversation Detail DTO
    /// </summary>
    public class ConversationDetailDto
    {
        public long Id { get; set; }
        public long? RequestId { get; set; }
        public string? RequestTitle { get; set; }
        public long? OrderId { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Participants
        public ParticipantDto? Student { get; set; }
        public ParticipantDto? Mentor { get; set; }

        // Messages
        public List<MessageDto>? Messages { get; set; }
        public int TotalMessages { get; set; }
    }

    /// <summary>
    /// Participant DTO
    /// </summary>
    public class ParticipantDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    /// <summary>
    /// Message DTO
    /// </summary>
    public class MessageDto
    {
        public long Id { get; set; }
        public long ConversationId { get; set; }
        public long SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatar { get; set; }
        public string? Content { get; set; }
        public MessageType MessageType { get; set; }
        public string MessageTypeDisplay => MessageType.ToString();
        public bool IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsOwn { get; set; } // Whether this is the current user's message

        // Files (if any)
        public List<MessageFileDto>? Files { get; set; }
    }

    /// <summary>
    /// Message File DTO
    /// </summary>
    public class MessageFileDto
    {
        public long Id { get; set; }
        public string? FileUrl { get; set; }
        public string? FileType { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string FileSizeDisplay => FormatFileSize(FileSize ?? 0);

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Send Message Request
    /// </summary>
    public class SendMessageDto
    {
        [Required]
        public long ConversationId { get; set; }

        [Required(ErrorMessage = "Message content is required")]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        public MessageType MessageType { get; set; } = MessageType.Text;
    }

    /// <summary>
    /// Send Message with Files Request
    /// </summary>
    public class SendMessageWithFileDto
    {
        [Required]
        public long ConversationId { get; set; }

        public string? Content { get; set; }

        public List<MessageFileUploadDto>? Files { get; set; }
    }

    /// <summary>
    /// File Upload DTO
    /// </summary>
    public class MessageFileUploadDto
    {
        [Required]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        public string FileType { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }
    }

    /// <summary>
    /// Create/Open Conversation Request
    /// </summary>
    public class CreateConversationDto
    {
        public long? RequestId { get; set; }
        public long? OrderId { get; set; }

        [Required]
        public long ParticipantId { get; set; }
    }

    /// <summary>
    /// Get Messages Request (paginated)
    /// </summary>
    public class GetMessagesDto
    {
        [Required]
        public long ConversationId { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public long? BeforeMessageId { get; set; } // Load messages before this message
    }
}
