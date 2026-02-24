using MentorEdu.DTOs.AIChat;

namespace MentorEdu.Services.Interfaces
{
    public interface IAIChatService
    {
        /// <summary>
        /// Send a message and get AI response
        /// </summary>
        Task<AIChatResponseDto> SendMessageAsync(long? userId, SendAIChatMessageDto request);

        /// <summary>
        /// Get all chat sessions for a user
        /// </summary>
        Task<List<AIChatSessionDto>> GetSessionsAsync(long? userId);

        /// <summary>
        /// Get all messages in a specific session
        /// </summary>
        Task<AIChatHistoryDto> GetSessionHistoryAsync(string sessionId, long? userId);

        /// <summary>
        /// Delete a chat session
        /// </summary>
        Task DeleteSessionAsync(string sessionId, long? userId);
    }
}
