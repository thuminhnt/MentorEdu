using MentorEdu.DTOs.AIChat;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class AIChatService : IAIChatService
    {
        private readonly IAIChatRepository _chatRepository;
        private readonly IGeminiAiService _geminiService;
        private readonly ILogger<AIChatService> _logger;

        public AIChatService(
            IAIChatRepository chatRepository,
            IGeminiAiService geminiService,
            ILogger<AIChatService> logger)
        {
            _chatRepository = chatRepository;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<AIChatResponseDto> SendMessageAsync(long? userId, SendAIChatMessageDto request)
        {
            // Generate session ID if not provided
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            // Save user message
            var userMessage = new AIChat
            {
                UserId = userId,
                SessionId = sessionId,
                Role = "user",
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };
            await _chatRepository.CreateAsync(userMessage);

            // Get conversation history for context (limited to 4 messages to save quota)
            var recentMessages = await _chatRepository.GetRecentMessagesAsync(sessionId, 4);
            var conversationHistory = recentMessages
                .Where(m => m.Id != userMessage.Id) // Exclude current message
                .Select(m => new ChatMessage
                {
                    Role = m.Role,
                    Content = m.Content
                })
                .ToList();

            // Call Gemini AI
            string aiResponseText;
            try
            {
                aiResponseText = await _geminiService.GeneralAIChatAsync(request.Content, conversationHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response");
                if (ex.Message.Contains("RATE_LIMITED"))
                {
                    aiResponseText = "The AI ​​is busy, please try again in a few seconds.";
                }
                else
                {
                    aiResponseText = "Sorry, I'm experiencing a technical issue. Please try again later.";
                }
            }

            // Save AI response
            var aiMessage = new AIChat
            {
                UserId = userId,
                SessionId = sessionId,
                Role = "assistant",
                Content = aiResponseText,
                CreatedAt = DateTime.UtcNow
            };
            await _chatRepository.CreateAsync(aiMessage);

            return new AIChatResponseDto
            {
                SessionId = sessionId,
                UserMessage = MapToDto(userMessage),
                AiMessage = MapToDto(aiMessage)
            };
        }

        public async Task<List<AIChatSessionDto>> GetSessionsAsync(long? userId)
        {
            return await _chatRepository.GetUserSessionsAsync(userId);
        }

        public async Task<AIChatHistoryDto> GetSessionHistoryAsync(string sessionId, long? userId)
        {
            var messages = await _chatRepository.GetSessionMessagesAsync(sessionId, userId);
            
            return new AIChatHistoryDto
            {
                SessionId = sessionId,
                Messages = messages.Select(MapToDto).ToList()
            };
        }

        public async Task DeleteSessionAsync(string sessionId, long? userId)
        {
            await _chatRepository.DeleteSessionAsync(sessionId, userId);
        }

        private static AIChatMessageDto MapToDto(AIChat chat)
        {
            return new AIChatMessageDto
            {
                Id = chat.Id,
                Role = chat.Role,
                Content = chat.Content,
                CreatedAt = chat.CreatedAt
            };
        }
    }
}
