namespace MentorEdu.DTOs.AIChat
{
    /// <summary>
    /// Request DTO for sending a message to AI chatbot
    /// </summary>
    public class SendAIChatMessageDto
    {
        public string Content { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    /// <summary>
    /// Response DTO for a single AI chat message
    /// </summary>
    public class AIChatMessageDto
    {
        public Guid Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Response DTO for AI chat response (both user message and AI response)
    /// </summary>
    public class AIChatResponseDto
    {
        public string SessionId { get; set; } = string.Empty;
        public AIChatMessageDto UserMessage { get; set; } = null!;
        public AIChatMessageDto AiMessage { get; set; } = null!;
    }

    /// <summary>
    /// Response DTO for a chat session summary
    /// </summary>
    public class AIChatSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string FirstMessage { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public DateTime LastMessageAt { get; set; }
    }

    /// <summary>
    /// Response DTO for chat history with all messages in a session
    /// </summary>
    public class AIChatHistoryDto
    {
        public string SessionId { get; set; } = string.Empty;
        public List<AIChatMessageDto> Messages { get; set; } = new();
    }
}
