using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Notification;
using MentorEdu.Models;
using MentorEdu.Models.Enums;
using MentorEdu.Repositories.Interfaces;
using MentorEdu.Services.Common;
using MentorEdu.Services.Interfaces;

namespace MentorEdu.Services.Implementations
{
    public class NotificationService : BaseService, INotificationService
    {
        public NotificationService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public async Task<ServiceResult<PagedResponse<NotificationDto>>> GetNotificationsAsync(long userId, SearchNotificationDto searchDto)
        {
            var (notifications, totalCount) = await _unitOfWork.Notifications.GetPagedByUserAsync(
                userId, searchDto.PageNumber, searchDto.PageSize, searchDto.IsRead);

            var dtos = notifications.Select(MapToDto);
            var response = PagedResponse<NotificationDto>.Create(dtos, searchDto.PageNumber, searchDto.PageSize, totalCount);

            return ServiceResult<PagedResponse<NotificationDto>>.SuccessResult(response);
        }

        public async Task<ServiceResult<NotificationSummaryDto>> GetNotificationSummaryAsync(long userId)
        {
            var unreadCount = await _unitOfWork.Notifications.GetUnreadCountByUserIdAsync(userId);
            var recentNotifications = await _unitOfWork.Notifications.GetByUserIdAsync(userId);

            var summary = new NotificationSummaryDto
            {
                TotalUnread = unreadCount,
                RecentNotifications = recentNotifications
                    .Take(5)
                    .Select(MapToDto)
                    .ToList()
            };

            return ServiceResult<NotificationSummaryDto>.SuccessResult(summary);
        }

        public async Task<ServiceResult> MarkAsReadAsync(long userId, MarkNotificationReadDto request)
        {
            if (request.MarkAll)
            {
                await _unitOfWork.Notifications.MarkAllAsReadByUserIdAsync(userId);
            }
            else if (request.NotificationIds != null && request.NotificationIds.Any())
            {
                foreach (var notificationId in request.NotificationIds)
                {
                    var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
                    if (notification != null && notification.UserId == userId)
                    {
                        await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.SuccessResult("Marked as read successfully");
        }

        public async Task<ServiceResult> CreateNotificationAsync(long userId, NotificationType type, string title, string body, string? data = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                Data = data,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Notification created successfully");
        }

        public async Task<ServiceResult> DeleteNotificationAsync(long notificationId, long userId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            
            if (notification == null)
                return ServiceResult.NotFoundResult("Notification not found");

            if (notification.UserId != userId)
                return ServiceResult.ForbiddenResult("You do not have permission to delete this notification");

            _unitOfWork.Notifications.Remove(notification);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult("Notification deleted successfully");
        }

        /// <summary>
        /// Notify mentors with matching specializations when a post contains relevant hashtags.
        /// Flow: Post hashtags → CategoryHashtag mapping → Category → MentorCategory → Mentor → Notification
        /// </summary>
        public async Task<ServiceResult> NotifyMentorsAboutRelevantPostAsync(long postId, List<string> hashtags)
        {
            if (hashtags == null || !hashtags.Any())
                return ServiceResult.SuccessResult("No hashtags provided");

            // Get post details
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);
            if (post == null)
                return ServiceResult.NotFoundResult("Post not found");

            // Step 1: Find category IDs mapped to these hashtags
            var categoryIds = await _unitOfWork.CategoryHashtags.GetCategoryIdsByHashtagNamesAsync(hashtags);
            if (!categoryIds.Any())
                return ServiceResult.SuccessResult("No matching categories found for hashtags");

            // Step 2: Find mentors who have these categories as specializations
            var mentorIds = new HashSet<long>();
            foreach (var categoryId in categoryIds)
            {
                var mentorCategories = await _unitOfWork.MentorProfiles.GetMentorIdsByCategoryIdAsync(categoryId);
                foreach (var mentorId in mentorCategories)
                {
                    // Don't notify the post author
                    if (mentorId != post.AuthorId)
                    {
                        mentorIds.Add(mentorId);
                    }
                }
            }

            if (!mentorIds.Any())
                return ServiceResult.SuccessResult("No mentors found with matching specializations");

            // Step 3: Create notifications for each mentor
            var hashtagsDisplay = string.Join(", ", hashtags.Take(3).Select(h => $"#{h}"));
            if (hashtags.Count > 3) hashtagsDisplay += "...";

            foreach (var mentorId in mentorIds)
            {
                var notification = new Notification
                {
                    UserId = mentorId,
                    Type = NotificationType.RelevantPost,
                    Title = "New relevant post in your expertise area",
                    Body = $"A new post about {hashtagsDisplay} has been published that matches your specializations.",
                    Data = System.Text.Json.JsonSerializer.Serialize(new { postId, hashtags }),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Notifications.AddAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.SuccessResult($"Notified {mentorIds.Count} mentor(s) about relevant post");
        }

        #region Private Methods

        private NotificationDto MapToDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Body = notification.Body,
                Data = notification.Data,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }

        #endregion
    }
}
