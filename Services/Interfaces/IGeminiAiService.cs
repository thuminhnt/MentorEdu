using MentorEdu.DTOs.User;

namespace MentorEdu.Services.Interfaces
{
    public interface IGeminiAiService
    {
        /// <summary>
        /// Analyze mentor application using Gemini AI
        /// Returns confidence score (0-100) and reasoning
        /// </summary>
        Task<GeminiAnalysisResult> AnalyzeMentorApplicationAsync(
            long userId,
            ApplyAsMentorRequest request,
            string certificationFileUrls
        );

        /// <summary>
        /// Check if Gemini AI is properly configured
        /// </summary>
        bool IsConfigured();

        /// <summary>
        /// Send a general chat message to Gemini AI (for chatbot)
        /// </summary>
        /// <param name="userMessage">The user's message</param>
        /// <param name="conversationHistory">Previous messages for context</param>
        /// <returns>AI response text</returns>
        Task<string> GeneralAIChatAsync(string userMessage, List<ChatMessage>? conversationHistory = null);
    }

    /// <summary>
    /// Represents a chat message in conversation history
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class GeminiAnalysisResult
    {
        /// <summary>
        /// AI confidence score (0-100)
        /// >= 80: Auto approve
        /// < 40: Auto reject
        /// 40-79: Pending manual review
        /// </summary>
        public decimal ConfidenceScore { get; set; }

        /// <summary>
        /// Recommended action: "AUTO_APPROVE", "AUTO_REJECT", or "MANUAL_REVIEW"
        /// </summary>
        public string RecommendedAction { get; set; } = string.Empty;

        /// <summary>
        /// Detailed reasoning from AI
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;

        /// <summary>
        /// Key points extracted from application (strengths)
        /// </summary>
        public List<string> KeyPoints { get; set; } = new();

        /// <summary>
        /// Potential red flags detected
        /// </summary>
        public List<string> RedFlags { get; set; } = new();

        /// <summary>
        /// AI model version used
        /// </summary>
        public string ModelVersion { get; set; } = "gemini-1.5-flash";
    }
}
