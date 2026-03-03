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
            // Tạo session ID nếu chưa có
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            // Lưu tin nhắn của người dùng vào database
            var userMessage = new AIChat
            {
                UserId = userId,
                SessionId = sessionId,
                Role = "user",
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };
            await _chatRepository.CreateAsync(userMessage);

            // Lấy lịch sử hội thoại để cung cấp ngữ cảnh (giới hạn 2 tin nhắn gần nhất để tiết kiệm hạn mức)
            var recentMessages = await _chatRepository.GetRecentMessagesAsync(sessionId, 2);
            var conversationHistory = recentMessages
                .Where(m => m.Id != userMessage.Id) // Loại bỏ tin nhắn hiện tại ra khỏi lịch sử
                .Select(m => new ChatMessage
                {
                    Role = m.Role,
                    Content = m.Content
                })
                .ToList();

            // Gọi Gemini AI để sinh phản hồi
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

            // Lưu phản hồi của AI vào database
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
