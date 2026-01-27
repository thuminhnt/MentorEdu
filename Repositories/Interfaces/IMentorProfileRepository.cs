using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IMentorProfileRepository : IRepository<MentorProfile>
    {
        Task<MentorProfile?> GetByUserIdAsync(long userId);
        Task<MentorProfile?> GetByUserIdWithDetailsAsync(long userId);
        Task<IEnumerable<MentorProfile>> GetTopRatedMentorsAsync(int count);
        Task<IEnumerable<MentorProfile>> GetMentorsByCategoryAsync(long categoryId);
        
        /// <summary>
        /// Get mentor user IDs who have the specified category as specialization.
        /// </summary>
        Task<IEnumerable<long>> GetMentorIdsByCategoryIdAsync(long categoryId);
    }
}
