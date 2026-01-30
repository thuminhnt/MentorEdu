using MentorEdu.DTOs.WorkSession;
using MentorEdu.Models;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface IWorkSessionService
    {
        Task<ServiceResult<WorkSessionStartResult>> StartSessionAsync(long orderId, long mentorId, long studentId);
        Task<ServiceResult<WorkSession>> GetSessionAsync(long sessionId);
        Task<ServiceResult<WorkSession>> PauseSessionAsync(long sessionId);
        Task<ServiceResult<WorkSession>> EndSessionAsync(long sessionId);
        Task<ServiceResult<WorkSession?>> GetActiveSessionByConversationAsync(long conversationId);
    }
}