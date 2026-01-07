using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IConversationRepository : IRepository<Conversation>
    {
        Task<Conversation?> GetByIdWithMessagesAsync(long id);
        Task<Conversation?> GetByRequestIdAsync(long requestId, long studentId, long mentorId);
        Task<Conversation?> GetByOrderIdAsync(long orderId);
        Task<IEnumerable<Conversation>> GetByUserIdAsync(long userId);
        Task<Conversation?> GetOrCreateConversationAsync(long? requestId, long? orderId, long studentId, long mentorId);
    }
}
