using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<IEnumerable<Message>> GetByConversationIdAsync(long conversationId);
        Task<(IEnumerable<Message> Items, int TotalCount)> GetPagedByConversationAsync(
            long conversationId,
            int pageNumber,
            int pageSize);
        Task<int> GetUnreadCountByUserAsync(long userId);
        Task<int> GetUnreadCountByConversationAsync(long conversationId, long userId);
        Task MarkAsReadAsync(long conversationId, long userId);
    }
}
