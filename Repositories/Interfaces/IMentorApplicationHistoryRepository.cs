using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IMentorApplicationHistoryRepository : IRepository<MentorApplicationHistory>
    {
        /// <summary>
        /// Get all history records for a specific user
        /// </summary>
        Task<IEnumerable<MentorApplicationHistory>> GetByUserIdAsync(long userId);

        /// <summary>
        /// Get submission count for a user
        /// </summary>
        Task<int> GetSubmissionCountAsync(long userId);

        /// <summary>
        /// Get history with filters for admin/export
        /// </summary>
        Task<(IEnumerable<MentorApplicationHistory> Items, int TotalCount)> GetFilteredAsync(
            int pageNumber,
            int pageSize,
            ApplicationAction? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Get all approved/rejected records for ML training export
        /// </summary>
        Task<IEnumerable<MentorApplicationHistory>> GetForMLExportAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null);
    }
}
