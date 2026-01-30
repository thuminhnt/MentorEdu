using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ICommunityPostRepository : IRepository<CommunityPost>
    {
        IQueryable<CommunityPost> Query();
        Task<CommunityPost?> GetByIdWithCommentsAsync(long id);
        Task<IEnumerable<CommunityPost>> GetByAuthorIdAsync(long authorId);
        Task<(IEnumerable<CommunityPost> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null);
        Task<(IEnumerable<CommunityPost> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            List<long> authorIds);
        Task<IEnumerable<CommunityPost>> GetRecentPostsAsync(int count);
    }
}
