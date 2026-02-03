using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Groups
{
    public class SendGroupMessageDto
    {
        public long GroupId { get; set; }
        public string? Content { get; set; }
        public MessageType MessageType { get; set; } = MessageType.Text;
    }
}
