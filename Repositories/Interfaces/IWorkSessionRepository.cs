using MentorEdu.Models;
using MentorEdu.Models.Enums;

namespace MentorEdu.Repositories.Interfaces
{
    public interface IWorkSessionRepository : IRepository<WorkSession>
    {
        Task<WorkSession?> GetByIdWithTrackingsAsync(long id);
        Task<IEnumerable<WorkSession>> GetByOrderIdAsync(long orderId);
        Task<IEnumerable<WorkSession>> GetByMentorIdAsync(long mentorId);
        Task<IEnumerable<WorkSession>> GetByStudentIdAsync(long studentId);
        Task<WorkSession?> GetActiveSessionByOrderAsync(long orderId);
        Task<int> GetTotalMinutesByOrderAsync(long orderId);
    }
}
