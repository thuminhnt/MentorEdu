using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Review>> GetByOrderIdAsync(long orderId)
        {
            return await _dbSet
                .Where(r => r.OrderId == orderId)
                .Include(r => r.FromUser)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.ToUser)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetByToUserIdAsync(long toUserId)
        {
            return await _dbSet
                .Where(r => r.ToUserId == toUserId)
                .Include(r => r.FromUser)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetByFromUserIdAsync(long fromUserId)
        {
            return await _dbSet
                .Where(r => r.FromUserId == fromUserId)
                .Include(r => r.ToUser)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingByUserAsync(long userId)
        {
            var reviews = await _dbSet
                .Where(r => r.ToUserId == userId)
                .ToListAsync();

            if (!reviews.Any())
                return 0;

            return reviews.Average(r => r.Rating);
        }

        public async Task<(IEnumerable<Review> Items, int TotalCount)> GetPagedByUserAsync(
            long userId,
            int pageNumber,
            int pageSize)
        {
            var query = _dbSet.Where(r => r.ToUserId == userId);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(r => r.FromUser)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> HasReviewedAsync(long orderId, long fromUserId)
        {
            return await _dbSet.AnyAsync(r => r.OrderId == orderId && r.FromUserId == fromUserId);
        }
    }
}
