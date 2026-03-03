using System.Collections.Concurrent;
using MentorEdu.DTOs.Chat;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    /// <summary>
    /// Xử lý toàn bộ logic nghiệp vụ cho các yêu cầu hành động làm việc (Bắt đầu / Tạm dừng / Kết thúc / Hoàn thành đơn hàng).
    /// <para>
    /// Dictionary <see cref="_pendingWorkActions"/> được khai báo <c>static</c> để chia sẻ trạng thái
    /// giữa tất cả các instance Scoped (mỗi kết nối Hub), thay vì giữ static trên <c>ChatHub</c>.
    /// </para>
    /// </summary>
    public class WorkActionService : IWorkActionService
    {
        // Trạng thái dùng chung — cố ý dùng static (một dictionary cho toàn bộ kết nối trong tiến trình)
        private static readonly ConcurrentDictionary<string, PendingWorkActionState> _pendingWorkActions = new();

        private readonly IChatService _chatService;
        private readonly IWorkSessionService _workSessionService;
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WorkActionService> _logger;

        public WorkActionService(
            IChatService chatService,
            IWorkSessionService workSessionService,
            IOrderService orderService,
            INotificationService notificationService,
            ILogger<WorkActionService> logger)
        {
            _chatService = chatService;
            _workSessionService = workSessionService;
            _orderService = orderService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        #region API công khai — Khởi tạo yêu cầu
        // ─────────────────────────────────────────────────────────────────────

        public async Task<WorkActionPendingResult> RequestStartWorkAsync(
            long requesterId, long conversationId, long orderId, long mentorId, long studentId)
        {
            if (!await _chatService.UserHasAccessToConversationAsync(requesterId, conversationId))
                return WorkActionPendingResult.Fail("You do not have access to this conversation");

            var convResult = await _chatService.GetConversationByIdAsync(conversationId);
            if (!convResult.Success || convResult.Data == null)
                return WorkActionPendingResult.Fail("Conversation not found");

            if (convResult.Data.Mentor?.Id != mentorId || convResult.Data.Student?.Id != studentId)
                return WorkActionPendingResult.Fail("Conversation participants do not match request");

            if (requesterId != mentorId && requesterId != studentId)
                return WorkActionPendingResult.Fail("Invalid requester");

            var targetUserId = requesterId == mentorId ? studentId : mentorId;
            var requestId = Guid.NewGuid().ToString("N");

            var pending = new PendingWorkActionState
            {
                RequestId = requestId,
                ActionType = WorkActionTypeDto.Start,
                ConversationId = conversationId,
                OrderId = orderId,
                SessionId = null,
                RequestedByUserId = requesterId,
                TargetUserId = targetUserId,
                MentorId = mentorId,
                StudentId = studentId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _pendingWorkActions[requestId] = pending;

            _logger.LogInformation("Work action Start requested by {UserId} for order {OrderId} in conv {ConvId}",
                requesterId, orderId, conversationId);

            return ToResult(pending);
        }

        public Task<WorkActionPendingResult> RequestPauseWorkAsync(long requesterId, long conversationId, long sessionId)
            => RequestWorkActionAsync(requesterId, conversationId, sessionId, WorkActionTypeDto.Pause);

        public Task<WorkActionPendingResult> RequestEndWorkAsync(long requesterId, long conversationId, long sessionId)
            => RequestWorkActionAsync(requesterId, conversationId, sessionId, WorkActionTypeDto.End);

        public async Task<WorkActionPendingResult> RequestCompleteOrderAsync(
            long requesterId, long conversationId, long orderId)
        {
            if (!await _chatService.UserHasAccessToConversationAsync(requesterId, conversationId))
                return WorkActionPendingResult.Fail("You do not have access to this conversation");

            var convResult = await _chatService.GetConversationByIdAsync(conversationId);
            if (!convResult.Success || convResult.Data == null)
                return WorkActionPendingResult.Fail("Conversation not found");

            var isMentor = convResult.Data.Mentor?.Id == requesterId;
            var isStudent = convResult.Data.Student?.Id == requesterId;
            if (!isMentor && !isStudent)
                return WorkActionPendingResult.Fail("Invalid requester");

            var otherUserId = isMentor ? convResult.Data.Student?.Id : convResult.Data.Mentor?.Id;
            if (otherUserId == null)
                return WorkActionPendingResult.Fail("Partner not found");

            // Ngăn trùng lặp yêu cầu hoàn thành đang chờ cho cùng một cuộc trò chuyện
            var existing = _pendingWorkActions.Values
                .FirstOrDefault(a => a.ConversationId == conversationId && a.ActionType == WorkActionTypeDto.Complete);
            if (existing != null)
                return WorkActionPendingResult.Fail("A completion request is already pending");

            var requestId = Guid.NewGuid().ToString("N");
            var pending = new PendingWorkActionState
            {
                RequestId = requestId,
                ActionType = WorkActionTypeDto.Complete,
                ConversationId = conversationId,
                OrderId = orderId,
                SessionId = null,
                RequestedByUserId = requesterId,
                TargetUserId = otherUserId.Value,
                MentorId = convResult.Data.Mentor?.Id,
                StudentId = convResult.Data.Student?.Id,
                CreatedAtUtc = DateTime.UtcNow
            };

            _pendingWorkActions[requestId] = pending;

            // Gửi tin nhắn hệ thống để cả hai người dùng thấy trong lịch sử chat
            var requesterName = isMentor
                ? (convResult.Data.Mentor?.FullName ?? "Mentor")
                : (convResult.Data.Student?.FullName ?? "Student");

            var systemContent = $"{requesterName} has requested to complete the order. Waiting for confirmation...";
            var systemMsgResult = await _chatService.SendMessageAsync(requesterId, new SendMessageDto
            {
                ConversationId = conversationId,
                Content = systemContent,
                MessageType = MessageType.System
            });

            // Gửi thông báo đẩy để người dùng kia nhận được kể cả khi offline
            try
            {
                await _notificationService.CreateNotificationAsync(
                    otherUserId.Value,
                    NotificationType.OrderCompleted,
                    "Order Completion Request",
                    $"{requesterName} has requested to complete the order. Please open chat to respond.",
                    $"{{\"conversationId\":{conversationId},\"orderId\":{orderId}}}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send completion notification to user {UserId}", otherUserId.Value);
            }

            var result = ToResult(pending);
            result.SystemMessageDto = systemMsgResult.Success ? systemMsgResult.Data : null;
            return result;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region API công khai — Xử lý phản hồi
        // ─────────────────────────────────────────────────────────────────────

        public async Task<WorkActionResponseResult> RespondWorkActionAsync(
            string requestId, long userId, bool accept)
        {
            if (!_pendingWorkActions.TryRemove(requestId, out var pending))
                return WorkActionResponseResult.Fail("Request not found or already handled");

            if (pending.TargetUserId != userId)
            {
                // Đặt lại vào dictionary để yêu cầu vẫn hợp lệ cho đúng người nhận
                _pendingWorkActions.TryAdd(requestId, pending);
                return WorkActionResponseResult.Fail("You are not allowed to respond to this request");
            }

            if (!accept)
            {
                return new WorkActionResponseResult
                {
                    Success = true,
                    Accepted = false,
                    ActionType = pending.ActionType,
                    ConversationId = pending.ConversationId,
                    RequestedByUserId = pending.RequestedByUserId,
                    RequestId = requestId
                };
            }

            // ── Nhánh chấp nhận yêu cầu ─────────────────────────────────────────
            switch (pending.ActionType)
            {
                case WorkActionTypeDto.Start:
                {
                    if (!pending.OrderId.HasValue || pending.MentorId == null || pending.StudentId == null)
                        return WorkActionResponseResult.Fail("Missing session details");

                    var startResult = await _workSessionService.StartSessionAsync(
                        pending.OrderId.Value, pending.MentorId.Value, pending.StudentId.Value);

                    if (!startResult.Success || startResult.Data == null)
                        return WorkActionResponseResult.Fail(startResult.Message ?? "Failed to start session");

                    return new WorkActionResponseResult
                    {
                        Success = true, Accepted = true,
                        ActionType = pending.ActionType,
                        ConversationId = pending.ConversationId,
                        RequestedByUserId = pending.RequestedByUserId,
                        RequestId = requestId,
                        SessionId = startResult.Data.SessionId,
                        OrderId = pending.OrderId,
                        StartTime = startResult.Data.StartTime
                    };
                }

                case WorkActionTypeDto.Pause:
                {
                    if (!pending.SessionId.HasValue)
                        return WorkActionResponseResult.Fail("Missing session id");

                    var pauseResult = await _workSessionService.PauseSessionAsync(pending.SessionId.Value);
                    if (!pauseResult.Success || pauseResult.Data == null)
                        return WorkActionResponseResult.Fail(pauseResult.Message ?? "Failed to pause session");

                    return new WorkActionResponseResult
                    {
                        Success = true, Accepted = true,
                        ActionType = pending.ActionType,
                        ConversationId = pending.ConversationId,
                        RequestedByUserId = pending.RequestedByUserId,
                        RequestId = requestId,
                        SessionId = pauseResult.Data.Id,
                        OrderId = pauseResult.Data.OrderId,
                        TotalMinutes = pauseResult.Data.TotalMinutes
                    };
                }

                case WorkActionTypeDto.End:
                {
                    if (!pending.SessionId.HasValue)
                        return WorkActionResponseResult.Fail("Missing session id");

                    var endResult = await _workSessionService.EndSessionAsync(pending.SessionId.Value);
                    if (!endResult.Success || endResult.Data == null)
                        return WorkActionResponseResult.Fail(endResult.Message ?? "Failed to end session");

                    return new WorkActionResponseResult
                    {
                        Success = true, Accepted = true,
                        ActionType = pending.ActionType,
                        ConversationId = pending.ConversationId,
                        RequestedByUserId = pending.RequestedByUserId,
                        RequestId = requestId,
                        SessionId = endResult.Data.Id,
                        OrderId = endResult.Data.OrderId,
                        TotalMinutes = endResult.Data.TotalMinutes,
                        EndTime = endResult.Data.EndTime
                    };
                }

                case WorkActionTypeDto.Complete:
                {
                    if (!pending.OrderId.HasValue)
                        return WorkActionResponseResult.Fail("Missing order id");

                    var completeResult = await _orderService.CompleteOrderAsync(pending.OrderId.Value, userId);
                    if (!completeResult.Success)
                        return WorkActionResponseResult.Fail(completeResult.Message ?? "Failed to complete order");

                    // Gửi tin nhắn hệ thống xác nhận hoàn thành đơn hàng
                    var sysMsg = await _chatService.SendMessageAsync(userId, new SendMessageDto
                    {
                        ConversationId = pending.ConversationId,
                        Content = " Order has been completed successfully! Thank you for using EduMentor.",
                        MessageType = MessageType.System
                    });

                    return new WorkActionResponseResult
                    {
                        Success = true, Accepted = true,
                        ActionType = pending.ActionType,
                        ConversationId = pending.ConversationId,
                        RequestedByUserId = pending.RequestedByUserId,
                        RequestId = requestId,
                        OrderId = pending.OrderId,
                        CompletedBy = pending.RequestedByUserId,
                        SystemMessageDto = sysMsg.Success ? sysMsg.Data : null
                    };
                }

                default:
                    return WorkActionResponseResult.Fail("Unknown action type");
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region API công khai — Truy vấn trạng thái
        // ─────────────────────────────────────────────────────────────────────

        public IEnumerable<PendingWorkActionState> GetPendingActionsForUser(long userId, long conversationId)
            => _pendingWorkActions.Values
                .Where(a => a.TargetUserId == userId && a.ConversationId == conversationId);

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Hàm hỗ trợ nội bộ
        // ─────────────────────────────────────────────────────────────────────

        private async Task<WorkActionPendingResult> RequestWorkActionAsync(
            long requesterId, long conversationId, long sessionId, WorkActionTypeDto actionType)
        {
            if (!await _chatService.UserHasAccessToConversationAsync(requesterId, conversationId))
                return WorkActionPendingResult.Fail("You do not have access to this conversation");

            var convResult = await _chatService.GetConversationByIdAsync(conversationId);
            if (!convResult.Success || convResult.Data == null)
                return WorkActionPendingResult.Fail("Conversation not found");

            var sessionResult = await _workSessionService.GetSessionAsync(sessionId);
            if (!sessionResult.Success || sessionResult.Data == null)
                return WorkActionPendingResult.Fail(sessionResult.Message ?? "Session not found");

            var session = sessionResult.Data;
            if (convResult.Data.Mentor?.Id != session.MentorId || convResult.Data.Student?.Id != session.StudentId)
                return WorkActionPendingResult.Fail("Conversation participants do not match session");

            if (requesterId != session.MentorId && requesterId != session.StudentId)
                return WorkActionPendingResult.Fail("Invalid requester");

            var targetUserId = requesterId == session.MentorId ? session.StudentId : session.MentorId;
            var requestId = Guid.NewGuid().ToString("N");

            var pending = new PendingWorkActionState
            {
                RequestId = requestId,
                ActionType = actionType,
                ConversationId = conversationId,
                OrderId = session.OrderId,
                SessionId = sessionId,
                RequestedByUserId = requesterId,
                TargetUserId = targetUserId,
                MentorId = session.MentorId,
                StudentId = session.StudentId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _pendingWorkActions[requestId] = pending;

            _logger.LogInformation("Work action {ActionType} requested by {UserId} for session {SessionId} in conv {ConvId}",
                actionType, requesterId, sessionId, conversationId);

            return ToResult(pending);
        }

        private static WorkActionPendingResult ToResult(PendingWorkActionState p) => new()
        {
            Success = true,
            RequestId = p.RequestId,
            ActionType = p.ActionType,
            ConversationId = p.ConversationId,
            OrderId = p.OrderId,
            SessionId = p.SessionId,
            TargetUserId = p.TargetUserId,
            RequestedByUserId = p.RequestedByUserId,
            MentorId = p.MentorId,
            StudentId = p.StudentId
        };

        #endregion
    }
}
