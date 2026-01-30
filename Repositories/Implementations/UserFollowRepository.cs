using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class UserFollowRepository : Repository<UserFollow>, IUserFollowRepository
    {
        public UserFollowRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserFollow?> GetByFollowerAndFollowingAsync(long followerId, long followingId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId);
        }

        public async Task<IEnumerable<UserFollow>> GetFollowersByUserIdAsync(long userId)
        {
            return await _dbSet
                .Where(uf => uf.FollowingId == userId)
                .Include(uf => uf.Follower)
                    .ThenInclude(u => u.UserProfile)
                .OrderByDescending(uf => uf.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserFollow>> GetFollowingByUserIdAsync(long userId)
        {
            return await _dbSet
                .Where(uf => uf.FollowerId == userId)
                .Include(uf => uf.Following)
                    .ThenInclude(u => u.UserProfile)
                .OrderByDescending(uf => uf.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<long>> GetFollowingIdsAsync(long userId)
        {
            return await _dbSet
                .Where(uf => uf.FollowerId == userId)
                .Select(uf => uf.FollowingId)
                .ToListAsync();
        }

        public async Task<bool> IsFollowingAsync(long followerId, long followingId)
        {
            return await _dbSet
                .AnyAsync(uf => uf.FollowerId == followerId && uf.FollowingId == followingId);
        }

        public async Task<int> GetFollowerCountAsync(long userId)
        {
            return await _dbSet.CountAsync(uf => uf.FollowingId == userId);
        }

        public async Task<int> GetFollowingCountAsync(long userId)
        {
            return await _dbSet.CountAsync(uf => uf.FollowerId == userId);
        }
    }
}
