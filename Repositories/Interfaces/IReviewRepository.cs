using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<IEnumerable<Review>> GetByOrderIdAsync(long orderId);
        Task<IEnumerable<Review>> GetByToUserIdAsync(long toUserId);
        Task<IEnumerable<Review>> GetByFromUserIdAsync(long fromUserId);
        Task<double> GetAverageRatingByUserAsync(long userId);
        Task<(IEnumerable<Review> Items, int TotalCount)> GetPagedByUserAsync(
            long userId,
            int pageNumber,
            int pageSize);
        Task<bool> HasReviewedAsync(long orderId, long fromUserId);
    }
}
