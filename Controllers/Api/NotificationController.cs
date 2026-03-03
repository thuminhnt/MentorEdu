using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Notification;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Lấy thông báo có phân trang
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<NotificationDto>>>> GetNotifications([FromQuery] SearchNotificationDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetNotificationsAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<NotificationDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<NotificationDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Lấy tóm tắt thông báo (số chưa đọc + gần đây)
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<NotificationSummaryDto>>> GetSummary()
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetNotificationSummaryAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<NotificationSummaryDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<NotificationSummaryDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        [HttpPost("mark-read")]
        public async Task<ActionResult<ApiResponse>> MarkAsRead([FromBody] MarkNotificationReadDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.MarkAsReadAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo đã đọc
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<ActionResult<ApiResponse>> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.MarkAsReadAsync(userId, new MarkNotificationReadDto { MarkAll = true });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Xóa một thông báo
        /// </summary>
        [HttpDelete("{notificationId}")]
        public async Task<ActionResult<ApiResponse>> DeleteNotification(long notificationId)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.DeleteNotificationAsync(notificationId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
