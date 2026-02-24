using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IGroupMessageRepository : IRepository<GroupMessage>
    {
        Task<List<GroupMessage>> GetMessagesByGroupIdAsync(long groupId, int pageNumber = 1, int pageSize = 50);
        Task<int> GetMessageCountAsync(long groupId);
        Task<GroupMessage?> GetByIdWithSenderAsync(long id);
        Task<GroupMessage?> GetLatestMessageByGroupIdAsync(long groupId);
        Task<int> GetUnreadCountAsync(long groupId, long userId, DateTime? lastReadAt);
    }
}
