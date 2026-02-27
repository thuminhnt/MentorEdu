using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ICommunityCommentRepository : IRepository<CommunityComment>
    {
        Task<IEnumerable<CommunityComment>> GetByPostIdAsync(long postId);
        Task<(IEnumerable<CommunityComment> Comments, int TotalCount)> GetPagedByPostIdAsync(long postId, int pageNumber, int pageSize);
        Task<IEnumerable<CommunityComment>> GetByAuthorIdAsync(long authorId);
        Task<int> GetCountByPostIdAsync(long postId);
    }
}
