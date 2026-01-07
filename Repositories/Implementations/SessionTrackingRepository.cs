using MentorEdu.Data;
using MentorEdu.Models;
using MentorEdu.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MentorEdu.Repositories.Implementations
{
    public class SessionTrackingRepository : Repository<SessionTracking>, ISessionTrackingRepository
    {
        public SessionTrackingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SessionTracking>> GetBySessionIdAsync(long sessionId)
        {
            return await _dbSet
                .Where(st => st.SessionId == sessionId)
                .OrderBy(st => st.StartAt)
                .ToListAsync();
        }

        public async Task<SessionTracking?> GetActiveTrackingBySessionAsync(long sessionId)
        {
            return await _dbSet
                .Where(st => st.SessionId == sessionId && st.EndAt == null)
                .FirstOrDefaultAsync();
        }
    }
}
