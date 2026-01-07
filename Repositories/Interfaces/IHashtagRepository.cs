using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IHashtagRepository : IRepository<Hashtag>
    {
        Task<Hashtag?> GetByNameAsync(string name);
        Task<IEnumerable<Hashtag>> SearchByNameAsync(string keyword);
        Task<IEnumerable<Hashtag>> GetPopularHashtagsAsync(int count);
    }
}
