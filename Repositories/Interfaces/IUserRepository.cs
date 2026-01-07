using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdWithProfilesAsync(long id);
        Task<IEnumerable<User>> GetMentorsAsync();
        Task<IEnumerable<User>> GetStudentsAsync();
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
        Task<bool> EmailExistsAsync(string email);
    }
}
