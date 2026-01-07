using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IUserProfileRepository : IRepository<UserProfile>
    {
        Task<UserProfile?> GetByUserIdAsync(long userId);
        Task<UserProfile?> GetByUserIdWithUserAsync(long userId);
    }
}
