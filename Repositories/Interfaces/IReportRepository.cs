using MentorEdu.Models;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<IEnumerable<Report>> GetByReporterIdAsync(long reporterId);
        Task<IEnumerable<Report>> GetByReportedUserIdAsync(long reportedUserId);
        Task<IEnumerable<Report>> GetPendingReportsAsync();
        Task<IEnumerable<Report>> GetByOrderIdAsync(long orderId);
    }
}
