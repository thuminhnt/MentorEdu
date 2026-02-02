using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IRequestRepository : IRepository<Request>
    {
        Task<Request?> GetByIdWithDetailsAsync(long id);
        Task<IEnumerable<Request>> GetByStudentIdAsync(long studentId);
        Task<IEnumerable<Request>> GetByMentorIdAsync(long mentorId);
        Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status);
        Task<IEnumerable<Request>> GetByCategoryIdAsync(long categoryId);
        Task<IEnumerable<Request>> GetOpenRequestsAsync();
        Task<Request?> GetPendingRequestByStudentAndMentorAsync(long studentId, long mentorId);
        Task<(IEnumerable<Request> Items, int TotalCount)> GetPagedWithFiltersAsync(
            int pageNumber,
            int pageSize,
            long? categoryId = null,
            RequestStatus? status = null,
            string? searchTerm = null);
    }
}
