using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class UserProfileRepository : Repository<UserProfile>, IUserProfileRepository
    {
        public UserProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserProfile?> GetByUserIdAsync(long userId)
        {
            return await _dbSet.FirstOrDefaultAsync(up => up.UserId == userId);
        }

        public async Task<UserProfile?> GetByUserIdWithUserAsync(long userId)
        {
            return await _dbSet
                .Include(up => up.User)
                .FirstOrDefaultAsync(up => up.UserId == userId);
        }
    }
}
