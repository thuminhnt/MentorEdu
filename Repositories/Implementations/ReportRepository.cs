using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class ReportRepository : Repository<Report>, IReportRepository
    {
        public ReportRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Report>> GetByReporterIdAsync(long reporterId)
        {
            return await _dbSet
                .Where(r => r.ReporterId == reporterId)
                .Include(r => r.ReportedUser)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetByReportedUserIdAsync(long reportedUserId)
        {
            return await _dbSet
                .Where(r => r.ReportedUserId == reportedUserId)
                .Include(r => r.Reporter)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetPendingReportsAsync()
        {
            return await _dbSet
                .Where(r => r.Status == "pending" || r.Status == null)
                .Include(r => r.Reporter)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.ReportedUser)
                    .ThenInclude(u => u.UserProfile)
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetByOrderIdAsync(long orderId)
        {
            return await _dbSet
                .Where(r => r.OrderId == orderId)
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.Handler)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
