using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ICommunityPostFileRepository : IRepository<CommunityPostFile>
    {
        Task<List<CommunityPostFile>> GetByPostIdAsync(long postId);
        Task DeleteByPostIdAsync(long postId);
    }
}
