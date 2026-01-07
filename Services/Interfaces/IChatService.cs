using MentorEdu.DTOs.Chat;
using MentorEdu.DTOs.Common;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IChatService
    {
        // Conversations
        Task<ServiceResult<ConversationDetailDto>> GetConversationByIdAsync(long conversationId);
        Task<ServiceResult<List<ConversationListDto>>> GetUserConversationsAsync(long userId);
        Task<ServiceResult<ConversationDetailDto>> GetOrCreateConversationAsync(long userId, CreateConversationDto request);
        Task<bool> UserHasAccessToConversationAsync(long userId, long conversationId);
        
        // Messages
        Task<ServiceResult<MessageDto>> SendMessageAsync(long userId, SendMessageDto request);
        Task<ServiceResult<MessageDto>> SendMessageWithFilesAsync(long userId, SendMessageWithFileDto request);
        Task<ServiceResult<PagedResponse<MessageDto>>> GetMessagesAsync(long userId, GetMessagesDto request);
        Task MarkMessagesAsReadAsync(long conversationId, long userId);
        Task<ServiceResult<int>> GetUnreadCountAsync(long userId);
    }
}
