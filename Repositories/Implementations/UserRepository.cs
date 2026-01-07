using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdWithProfilesAsync(long id)
        {
            return await _dbSet
                .Include(u => u.UserProfile)
                .Include(u => u.MentorProfile)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<User>> GetMentorsAsync()
        {
            return await _dbSet
                .Where(u => u.Role == UserRole.Mentor && u.IsActive)
                .Include(u => u.MentorProfile)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetStudentsAsync()
        {
            return await _dbSet
                .Where(u => u.Role == UserRole.Student && u.IsActive)
                .Include(u => u.UserProfile)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _dbSet
                .Where(u => u.Role == role && u.IsActive)
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }
    }
}
