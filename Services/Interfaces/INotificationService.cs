using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Notification;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Common;

namespace MentorEdu.Services.Interfaces
{
    public interface INotificationService
    {
        Task<ServiceResult<PagedResponse<NotificationDto>>> GetNotificationsAsync(long userId, SearchNotificationDto searchDto);
        Task<ServiceResult<NotificationSummaryDto>> GetNotificationSummaryAsync(long userId);
        Task<ServiceResult> MarkAsReadAsync(long userId, MarkNotificationReadDto request);
        Task<ServiceResult> CreateNotificationAsync(long userId, NotificationType type, string title, string body, string? data = null);
        Task<ServiceResult> DeleteNotificationAsync(long notificationId, long userId);
        
        /// <summary>
        /// Thông báo cho các mentor có chuyên môn phù hợp khi bài viết có danh mục hoặc hashtag liên quan.
        /// Luồng: CategoryId/Hashtags → MentorCategory/MentorHashtag → Mentor → Thông báo
        /// </summary>
        Task<ServiceResult> NotifyMentorsAboutRelevantPostAsync(long postId, long? categoryId, List<string> hashtags);
    }
}
