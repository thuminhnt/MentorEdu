namespace MentorEdu.DTOs.Groups
{
    public class GroupMessageDto
    {
        public long Id { get; set; }
        public long GroupId { get; set; }
        public long SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public string? Content { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
