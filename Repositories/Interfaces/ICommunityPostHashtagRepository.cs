using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ICommunityPostHashtagRepository : IRepository<CommunityPostHashtag>
    {
        new IQueryable<CommunityPostHashtag> Query();
        Task<List<CommunityPostHashtag>> GetByPostIdAsync(long postId);
        Task DeleteByPostIdAsync(long postId);
    }
}
