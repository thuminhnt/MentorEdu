using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IMentorProfileRepository : IRepository<MentorProfile>
    {
        Task<MentorProfile?> GetByUserIdAsync(long userId);
        Task<MentorProfile?> GetByUserIdWithDetailsAsync(long userId);
        Task<IEnumerable<MentorProfile>> GetTopRatedMentorsAsync(int count);
        Task<IEnumerable<MentorProfile>> GetMentorsByCategoryAsync(long categoryId);
    }
}
