using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IUserFollowRepository : IRepository<UserFollow>
    {
        Task<UserFollow?> GetByFollowerAndFollowingAsync(long followerId, long followingId);
        Task<IEnumerable<UserFollow>> GetFollowersByUserIdAsync(long userId);
        Task<IEnumerable<UserFollow>> GetFollowingByUserIdAsync(long userId);
        Task<IEnumerable<long>> GetFollowingIdsAsync(long userId);
        Task<bool> IsFollowingAsync(long followerId, long followingId);
        Task<int> GetFollowerCountAsync(long userId);
        Task<int> GetFollowingCountAsync(long userId);
    }
}
