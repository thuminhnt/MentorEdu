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
        /// Notify mentors with matching specializations when a post contains relevant category or hashtags.
        /// Flow: CategoryId/Hashtags → MentorCategory/MentorHashtag → Mentor → Notification
        /// </summary>
        Task<ServiceResult> NotifyMentorsAboutRelevantPostAsync(long postId, long? categoryId, List<string> hashtags);
    }
}
