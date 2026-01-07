using MentorEdu.Models.Enums;

namespace MentorEdu.DTOs.Notification
{
    /// <summary>
    /// Notification DTO
    /// </summary>
    public class NotificationDto
    {
        public long Id { get; set; }
        public NotificationType Type { get; set; }
        public string TypeDisplay => GetTypeDisplay();
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Data { get; set; }
        public bool IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string TimeAgo => GetTimeAgo();

        // Icon/Color based on type
        public string Icon => GetIcon();
        public string Color => GetColor();

        private string GetTypeDisplay()
        {
            return Type switch
            {
                NotificationType.System => "H? th?ng",
                NotificationType.RequestCreated => "Yêu c?u m?i",
                NotificationType.RequestAssigned => "Yêu c?u ???c giao",
                NotificationType.ProposalCreated => "?? xu?t m?i",
                NotificationType.ProposalAccepted => "?? xu?t ???c ch?p nh?n",
                NotificationType.OrderStarted => "??n hàng b?t ??u",
                NotificationType.OrderCompleted => "??n hàng hoàn thành",
                NotificationType.OrderDisputed => "??n hàng tranh ch?p",
                NotificationType.WalletTransaction => "Giao d?ch ví",
                _ => Type.ToString()
            };
        }

        private string GetIcon()
        {
            return Type switch
            {
                NotificationType.System => "bell",
                NotificationType.RequestCreated => "file-text",
                NotificationType.RequestAssigned => "user-check",
                NotificationType.ProposalCreated => "send",
                NotificationType.ProposalAccepted => "check-circle",
                NotificationType.OrderStarted => "play-circle",
                NotificationType.OrderCompleted => "check-square",
                NotificationType.OrderDisputed => "alert-triangle",
                NotificationType.WalletTransaction => "credit-card",
                _ => "bell"
            };
        }

        private string GetColor()
        {
            return Type switch
            {
                NotificationType.System => "gray",
                NotificationType.RequestCreated => "blue",
                NotificationType.RequestAssigned => "blue",
                NotificationType.ProposalCreated => "purple",
                NotificationType.ProposalAccepted => "green",
                NotificationType.OrderStarted => "blue",
                NotificationType.OrderCompleted => "green",
                NotificationType.OrderDisputed => "red",
                NotificationType.WalletTransaction => "orange",
                _ => "gray"
            };
        }

        private string GetTimeAgo()
        {
            if (!CreatedAt.HasValue) return "";

            var timeSpan = DateTime.UtcNow - CreatedAt.Value;

            if (timeSpan.TotalMinutes < 1) return "V?a xong";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút tr??c";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} gi? tr??c";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} ngày tr??c";
            if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)} tu?n tr??c";
            if (timeSpan.TotalDays < 365) return $"{(int)(timeSpan.TotalDays / 30)} tháng tr??c";

            return $"{(int)(timeSpan.TotalDays / 365)} n?m tr??c";
        }
    }

    /// <summary>
    /// Notification Summary
    /// </summary>
    public class NotificationSummaryDto
    {
        public int TotalUnread { get; set; }
        public List<NotificationDto>? RecentNotifications { get; set; }
    }

    /// <summary>
    /// Request filter notifications
    /// </summary>
    public class SearchNotificationDto
    {
        public NotificationType? Type { get; set; }
        public bool? IsRead { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Request ?ánh d?u ?ã ??c
    /// </summary>
    public class MarkNotificationReadDto
    {
        public List<long>? NotificationIds { get; set; }
        public bool MarkAll { get; set; } = false;
    }
}
