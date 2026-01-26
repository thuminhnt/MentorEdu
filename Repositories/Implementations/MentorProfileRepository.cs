using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class MentorProfileRepository : Repository<MentorProfile>, IMentorProfileRepository
    {
        public MentorProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<MentorProfile?> GetByUserIdAsync(long userId)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(mp => mp.UserId == userId);

            } catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<MentorProfile?> GetByUserIdWithDetailsAsync(long userId)
        {
            return await _dbSet
                .Include(mp => mp.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(mp => mp.User)
                    .ThenInclude(u => u.MentorCategories)
                        .ThenInclude(mc => mc.Category)
                .Include(mp => mp.User)
                    .ThenInclude(u => u.MentorHashtags)
                        .ThenInclude(mh => mh.Hashtag)
                .FirstOrDefaultAsync(mp => mp.UserId == userId);
        }

        public async Task<IEnumerable<MentorProfile>> GetTopRatedMentorsAsync(int count)
        {
            return await _dbSet
                .Include(mp => mp.User)
                    .ThenInclude(u => u.UserProfile)
                .Where(mp => mp.User.IsActive && mp.RatingCount > 0)
                .OrderByDescending(mp => mp.RatingAvg)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<MentorProfile>> GetMentorsByCategoryAsync(long categoryId)
        {
            return await _dbSet
                .Include(mp => mp.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(mp => mp.User)
                    .ThenInclude(u => u.MentorCategories)
                .Where(mp => mp.User.IsActive && 
                       mp.User.MentorCategories.Any(mc => mc.CategoryId == categoryId))
                .ToListAsync();
        }
    }
}
