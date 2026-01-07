using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class WorkSessionRepository : Repository<WorkSession>, IWorkSessionRepository
    {
        public WorkSessionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<WorkSession?> GetByIdWithTrackingsAsync(long id)
        {
            return await _dbSet
                .Include(ws => ws.SessionTrackings)
                .Include(ws => ws.Mentor)
                .Include(ws => ws.Student)
                .FirstOrDefaultAsync(ws => ws.Id == id);
        }

        public async Task<IEnumerable<WorkSession>> GetByOrderIdAsync(long orderId)
        {
            return await _dbSet
                .Where(ws => ws.OrderId == orderId)
                .Include(ws => ws.SessionTrackings)
                .OrderByDescending(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkSession>> GetByMentorIdAsync(long mentorId)
        {
            return await _dbSet
                .Where(ws => ws.MentorId == mentorId)
                .Include(ws => ws.Order)
                .Include(ws => ws.Student)
                .OrderByDescending(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkSession>> GetByStudentIdAsync(long studentId)
        {
            return await _dbSet
                .Where(ws => ws.StudentId == studentId)
                .Include(ws => ws.Order)
                .Include(ws => ws.Mentor)
                .OrderByDescending(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<WorkSession?> GetActiveSessionByOrderAsync(long orderId)
        {
            return await _dbSet
                .Where(ws => ws.OrderId == orderId && 
                       (ws.Status == WorkSessionStatus.Running || ws.Status == WorkSessionStatus.Paused))
                .Include(ws => ws.SessionTrackings)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetTotalMinutesByOrderAsync(long orderId)
        {
            return await _dbSet
                .Where(ws => ws.OrderId == orderId && ws.Status == WorkSessionStatus.Finished)
                .SumAsync(ws => ws.TotalMinutes ?? 0);
        }
    }
}
