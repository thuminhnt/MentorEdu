using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IRequestRepository : IRepository<Request>
    {
        Task<Request?> GetByIdWithDetailsAsync(long id);
        Task<IEnumerable<Request>> GetByStudentIdAsync(long studentId);
        Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status);
        Task<IEnumerable<Request>> GetByCategoryIdAsync(long categoryId);
        Task<IEnumerable<Request>> GetOpenRequestsAsync();
        Task<(IEnumerable<Request> Items, int TotalCount)> GetPagedWithFiltersAsync(
            int pageNumber,
            int pageSize,
            long? categoryId = null,
            RequestStatus? status = null,
            string? searchTerm = null);
    }
}
