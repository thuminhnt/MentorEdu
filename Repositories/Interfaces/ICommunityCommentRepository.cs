using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ICommunityCommentRepository : IRepository<CommunityComment>
    {
        Task<IEnumerable<CommunityComment>> GetByPostIdAsync(long postId);
        Task<IEnumerable<CommunityComment>> GetByAuthorIdAsync(long authorId);
        Task<int> GetCountByPostIdAsync(long postId);
    }
}
