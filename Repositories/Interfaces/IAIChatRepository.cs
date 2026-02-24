using MentorEdu.DTOs.AIChat;
using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IAIChatRepository
    {
        /// <summary>
        /// Add a new chat message
        /// </summary>
        Task<AIChat> CreateAsync(AIChat chat);

        /// <summary>
        /// Get all sessions for a user (for history list)
        /// </summary>
        Task<List<AIChatSessionDto>> GetUserSessionsAsync(long? userId, int limit = 20);

        /// <summary>
        /// Get all messages in a specific session
        /// </summary>
        Task<List<AIChat>> GetSessionMessagesAsync(string sessionId, long? userId);

        /// <summary>
        /// Get recent messages for conversation context (for AI context)
        /// </summary>
        Task<List<AIChat>> GetRecentMessagesAsync(string sessionId, int limit = 10);

        /// <summary>
        /// Delete a session and all its messages
        /// </summary>
        Task DeleteSessionAsync(string sessionId, long? userId);
    }
}
