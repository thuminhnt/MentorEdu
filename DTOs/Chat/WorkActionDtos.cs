namespace MentorEdu.DTOs.Chat
{
    /// <summary>
    /// Kết quả trả về sau khi yêu cầu hành động công việc được khởi tạo (Start/Pause/End/Complete).
    /// Hub sử dụng dữ liệu này để phát sóng sự kiện SignalR.
    /// </summary>
    public class WorkActionPendingResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public string RequestId { get; set; } = string.Empty;
        public WorkActionTypeDto ActionType { get; set; }
        public long ConversationId { get; set; }
        public long? OrderId { get; set; }
        public long? SessionId { get; set; }

        public long TargetUserId { get; set; }
        public long RequestedByUserId { get; set; }
        public long? MentorId { get; set; }
        public long? StudentId { get; set; }

        /// <summary>DTO tin nhắn hệ thống để phát lại qua ReceiveMessage (dùng bởi RequestCompleteOrder)</summary>
        public object? SystemMessageDto { get; set; }

        public static WorkActionPendingResult Fail(string error) => new() { Success = false, Error = error };
    }

    /// <summary>
    /// Kết quả trả về sau khi người dùng phản hồi yêu cầu hành động đang chờ.
    /// Hub sử dụng để phát sóng sự kiện phiên/đơn hàng phù hợp.
    /// </summary>
    public class WorkActionResponseResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public bool Accepted { get; set; }

        public WorkActionTypeDto ActionType { get; set; }
        public long ConversationId { get; set; }
        public long RequestedByUserId { get; set; }
        public string RequestId { get; set; } = string.Empty;

        // Cho trường hợp bắt đầu thành công
        public long? SessionId { get; set; }
        public long? OrderId { get; set; }
        public DateTime? StartTime { get; set; }

        // Cho trường hợp tạm dừng / kết thúc thành công
        public decimal? TotalMinutes { get; set; }
        public DateTime? EndTime { get; set; }

        // Cho trường hợp hoàn thành thành công
        public long? CompletedBy { get; set; }
        public object? SystemMessageDto { get; set; }

        public static WorkActionResponseResult Fail(string error) => new() { Success = false, Error = error };
    }

    /// <summary>Bản sao của enum WorkActionType, giữ trong DTOs layer để tách biệt Hub khỏi namespace Hubs.</summary>
    public enum WorkActionTypeDto
    {
        Start,
        Pause,
        End,
        Complete
    }

    /// <summary>
    /// Đại diện cho hành động công việc đang chờ xử lý, chờ bên kia chấp nhận hoặc từ chối.
    /// Được lưu trong ConcurrentDictionary tĩnh trong WorkActionService.
    /// </summary>
    public class PendingWorkActionState
    {
        public string RequestId { get; set; } = string.Empty;
        public WorkActionTypeDto ActionType { get; set; }
        public long ConversationId { get; set; }
        public long? OrderId { get; set; }
        public long? SessionId { get; set; }
        public long RequestedByUserId { get; set; }
        public long TargetUserId { get; set; }
        public long? MentorId { get; set; }
        public long? StudentId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
