using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetByUserIdAsync(long userId);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(long userId);
        Task<int> GetUnreadCountByUserIdAsync(long userId);
        Task MarkAsReadAsync(long notificationId);
        Task MarkAllAsReadByUserIdAsync(long userId);
        Task<(IEnumerable<Notification> Items, int TotalCount)> GetPagedByUserAsync(
            long userId,
            int pageNumber,
            int pageSize,
            bool? isRead = null);
    }
}
