using MentorEdu.DTOs.WorkSession;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class WorkSessionService : BaseService, IWorkSessionService
    {
        public WorkSessionService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public async Task<ServiceResult<WorkSessionStartResult>> StartSessionAsync(long orderId, long mentorId, long studentId)
        {
            var activeSession = await _unitOfWork.WorkSessions.GetActiveSessionByOrderAsync(orderId);
            if (activeSession != null)
                return ServiceResult<WorkSessionStartResult>.FailResult("A session is already in progress");

            var session = new WorkSession
            {
                OrderId = orderId,
                MentorId = mentorId,
                StudentId = studentId,
                StartTime = DateTime.UtcNow,
                Status = WorkSessionStatus.Running,
                TotalMinutes = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.WorkSessions.AddAsync(session);
            await _unitOfWork.SaveChangesAsync();

            var tracking = new SessionTracking
            {
                SessionId = session.Id,
                StartAt = DateTime.UtcNow
            };

            await _unitOfWork.SessionTrackings.AddAsync(tracking);
            await _unitOfWork.SaveChangesAsync();

            var result = new WorkSessionStartResult
            {
                SessionId = session.Id,
                StartTime = session.StartTime ?? DateTime.UtcNow
            };

            return ServiceResult<WorkSessionStartResult>.SuccessResult(result);
        }

        public async Task<ServiceResult<WorkSession>> GetSessionAsync(long sessionId)
        {
            var session = await _unitOfWork.WorkSessions.GetByIdWithTrackingsAsync(sessionId);
            if (session == null)
                return ServiceResult<WorkSession>.NotFoundResult("Session not found");

            return ServiceResult<WorkSession>.SuccessResult(session);
        }

        public async Task<ServiceResult<WorkSession>> PauseSessionAsync(long sessionId)
        {
            var session = await _unitOfWork.WorkSessions.GetByIdWithTrackingsAsync(sessionId);
            if (session == null)
                return ServiceResult<WorkSession>.NotFoundResult("Session not found");

            if (session.Status != WorkSessionStatus.Running)
                return ServiceResult<WorkSession>.FailResult("Session is not running");

            var activeTracking = await _unitOfWork.SessionTrackings.GetActiveTrackingBySessionAsync(sessionId);
            if (activeTracking != null)
            {
                activeTracking.EndAt = DateTime.UtcNow;
                activeTracking.DurationMin = (int)(DateTime.UtcNow - (activeTracking.StartAt ?? DateTime.UtcNow)).TotalMinutes;
            }

            var trackings = await _unitOfWork.SessionTrackings.GetBySessionIdAsync(sessionId);
            session.TotalMinutes = trackings.Sum(t => t.DurationMin ?? 0);
            session.Status = WorkSessionStatus.Paused;
            session.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WorkSessions.Update(session);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<WorkSession>.SuccessResult(session);
        }

        public async Task<ServiceResult<WorkSession>> EndSessionAsync(long sessionId)
        {
            var session = await _unitOfWork.WorkSessions.GetByIdWithTrackingsAsync(sessionId);
            if (session == null)
                return ServiceResult<WorkSession>.NotFoundResult("Session not found");

            if (session.Status != WorkSessionStatus.Running && session.Status != WorkSessionStatus.Paused)
                return ServiceResult<WorkSession>.FailResult("Session is not in a state that can be ended");

            if (session.Status == WorkSessionStatus.Running)
            {
                var activeTracking = await _unitOfWork.SessionTrackings.GetActiveTrackingBySessionAsync(sessionId);
                if (activeTracking != null)
                {
                    activeTracking.EndAt = DateTime.UtcNow;
                    activeTracking.DurationMin = (int)(DateTime.UtcNow - (activeTracking.StartAt ?? DateTime.UtcNow)).TotalMinutes;
                }
            }

            var trackings = await _unitOfWork.SessionTrackings.GetBySessionIdAsync(sessionId);
            session.TotalMinutes = trackings.Sum(t => t.DurationMin ?? 0);
            session.Status = WorkSessionStatus.Finished;
            session.EndTime = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WorkSessions.Update(session);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<WorkSession>.SuccessResult(session);
        }

        public async Task<ServiceResult<WorkSession?>> GetActiveSessionByConversationAsync(long conversationId)
        {
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
            if (conversation == null)
                return ServiceResult<WorkSession?>.NotFoundResult("Conversation not found");

            if (!conversation.OrderId.HasValue)
                return ServiceResult<WorkSession?>.SuccessResult(null);

            var session = await _unitOfWork.WorkSessions.GetActiveSessionByOrderAsync(conversation.OrderId.Value);
            return ServiceResult<WorkSession?>.SuccessResult(session);
        }
    }
}