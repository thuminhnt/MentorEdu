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
        /// Notify mentors with matching specializations when a post contains relevant category or hashtags.
        /// Flow: CategoryId/Hashtags → MentorCategory/MentorHashtag → Mentor → Notification
        /// </summary>
        public async Task<ServiceResult> NotifyMentorsAboutRelevantPostAsync(long postId, long? categoryId, List<string> hashtags)
        {
            // Get post details
            var post = await _unitOfWork.CommunityPosts.GetByIdAsync(postId);
            if (post == null)
                return ServiceResult.NotFoundResult("Post not found");

            var mentorIds = new HashSet<long>();

            // Step 1: Find mentors by CategoryId (direct match)
            if (categoryId.HasValue)
            {
                var mentorsByCategory = await _unitOfWork.MentorProfiles.GetMentorIdsByCategoryIdAsync(categoryId.Value);
                foreach (var mentorId in mentorsByCategory)
                {
                    if (mentorId != post.AuthorId)
                    {
                        mentorIds.Add(mentorId);
                    }
                }
            }

            // Step 2: Find mentors by hashtags (via CategoryHashtag mapping)
            if (hashtags != null && hashtags.Any())
            {
                var categoryIds = await _unitOfWork.CategoryHashtags.GetCategoryIdsByHashtagNamesAsync(hashtags);
                foreach (var catId in categoryIds)
                {
                    var mentorsByHashtagCategory = await _unitOfWork.MentorProfiles.GetMentorIdsByCategoryIdAsync(catId);
                    foreach (var mentorId in mentorsByHashtagCategory)
                    {
                        if (mentorId != post.AuthorId)
                        {
                            mentorIds.Add(mentorId);
                        }
                    }
                }
            }

            if (!mentorIds.Any())
                return ServiceResult.SuccessResult("No mentors found with matching specializations");

            // Step 3: Build display text
            var displayParts = new List<string>();
            if (categoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(categoryId.Value);
                if (category != null)
                {
                    displayParts.Add(category.Name);
                }
            }
            if (hashtags != null && hashtags.Any())
            {
                displayParts.AddRange(hashtags.Take(2).Select(h => $"#{h}"));
            }
            var displayText = string.Join(", ", displayParts.Take(3));
            if (displayParts.Count > 3) displayText += "...";

            // Step 4: Create notifications for each mentor
            foreach (var mentorId in mentorIds)
            {
                var notification = new Notification
                {
                    UserId = mentorId,
                    Type = NotificationType.RelevantPost,
                    Title = "New relevant post in your expertise area",
                    Body = $"A new post about {displayText} has been published that matches your specializations.",
                    Data = System.Text.Json.JsonSerializer.Serialize(new { postId, categoryId, hashtags }),
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
