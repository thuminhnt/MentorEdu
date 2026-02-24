using MentorEdu.Models.Enums;
using System.Text.Json.Serialization;

namespace MentorEdu.DTOs.Groups
{
    public class SendGroupMessageDto
    {
        public long GroupId { get; set; }
        public string? Content { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageType MessageType { get; set; } = MessageType.Text;
    }
}
