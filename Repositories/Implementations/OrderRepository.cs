using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Order?> GetByIdWithDetailsAsync(long id)
        {
            return await _dbSet
                .Include(o => o.Student)
                    .ThenInclude(s => s.UserProfile)
                .Include(o => o.Mentor)
                    .ThenInclude(m => m.UserProfile)
                .Include(o => o.Request)
                .Include(o => o.Proposal)
                .Include(o => o.Escrow)
                .Include(o => o.WorkSessions)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetByStudentIdAsync(long studentId)
        {
            return await _dbSet
                .Where(o => o.StudentId == studentId)
                .Include(o => o.Mentor)
                    .ThenInclude(m => m.UserProfile)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByMentorIdAsync(long mentorId)
        {
            return await _dbSet
                .Where(o => o.MentorId == mentorId)
                .Include(o => o.Student)
                    .ThenInclude(s => s.UserProfile)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _dbSet
                .Where(o => o.Status == status)
                .Include(o => o.Student)
                .Include(o => o.Mentor)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetActiveOrdersAsync()
        {
            return await _dbSet
                .Where(o => o.Status == OrderStatus.InProgress)
                .Include(o => o.Student)
                .Include(o => o.Mentor)
                .OrderByDescending(o => o.StartedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetCompletedOrdersAsync()
        {
            return await _dbSet
                .Where(o => o.Status == OrderStatus.Completed)
                .Include(o => o.Student)
                .Include(o => o.Mentor)
                .OrderByDescending(o => o.CompletedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetDisputedOrdersAsync()
        {
            return await _dbSet
                .Where(o => o.Status == OrderStatus.Disputed)
                .Include(o => o.Student)
                .Include(o => o.Mentor)
                .Include(o => o.Reports)
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueByMentorAsync(long mentorId)
        {
            return await _dbSet
                .Where(o => o.MentorId == mentorId && o.Status == OrderStatus.Completed)
                .SumAsync(o => o.TotalPrice ?? 0);
        }

        public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedByUserAsync(
            long userId,
            bool isMentor,
            int pageNumber,
            int pageSize,
            OrderStatus? status = null)
        {
            var query = isMentor
                ? _dbSet.Where(o => o.MentorId == userId)
                : _dbSet.Where(o => o.StudentId == userId);

            if (status.HasValue)
                query = query.Where(o => o.Status == status);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(o => o.Student)
                    .ThenInclude(s => s.UserProfile)
                .Include(o => o.Mentor)
                    .ThenInclude(m => m.UserProfile)
                .Include(o => o.Reviews)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
