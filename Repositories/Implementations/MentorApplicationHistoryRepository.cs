using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class MentorApplicationHistoryRepository : Repository<MentorApplicationHistory>, IMentorApplicationHistoryRepository
    {
        public MentorApplicationHistoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MentorApplicationHistory>> GetByUserIdAsync(long userId)
        {
            return await _dbSet
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetSubmissionCountAsync(long userId)
        {
            return await _dbSet
                .CountAsync(h => h.UserId == userId && h.Action == ApplicationAction.Submitted);
        }

        public async Task<(IEnumerable<MentorApplicationHistory> Items, int TotalCount)> GetFilteredAsync(
            int pageNumber,
            int pageSize,
            ApplicationAction? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _dbSet.AsQueryable();

            if (action.HasValue)
                query = query.Where(h => h.Action == action.Value);

            if (fromDate.HasValue)
                query = query.Where(h => h.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(h => h.CreatedAt <= toDate.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(h => h.User)
                    .ThenInclude(u => u!.UserProfile)
                .Include(h => h.ActionPerformedBy)
                    .ThenInclude(u => u!.UserProfile)
                .OrderByDescending(h => h.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<MentorApplicationHistory>> GetForMLExportAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _dbSet
                .Where(h => h.Action == ApplicationAction.Approved || 
                           h.Action == ApplicationAction.Rejected ||
                           h.Action == ApplicationAction.AutoApproved ||
                           h.Action == ApplicationAction.AutoRejected);

            if (fromDate.HasValue)
                query = query.Where(h => h.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(h => h.CreatedAt <= toDate.Value);

            return await query
                .Include(h => h.User)
                    .ThenInclude(u => u!.UserProfile)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }
    }
}
