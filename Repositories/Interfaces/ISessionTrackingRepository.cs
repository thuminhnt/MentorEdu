using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface ISessionTrackingRepository : IRepository<SessionTracking>
    {
        Task<IEnumerable<SessionTracking>> GetBySessionIdAsync(long sessionId);
        Task<SessionTracking?> GetActiveTrackingBySessionAsync(long sessionId);
    }
}
