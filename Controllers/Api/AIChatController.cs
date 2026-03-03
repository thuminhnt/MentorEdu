using MentorEdu.DTOs.AIChat;
using MentorEdu.DTOs.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIChatController : ControllerBase
    {
        private readonly IAIChatService _chatService;

        public AIChatController(IAIChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Lấy ID người dùng hiện tại từ claims
        /// </summary>
        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value
                           ?? User.FindFirst("UserId")?.Value;

            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Gửi tin nhắn đến chatbot AI
        /// POST /api/AIChat/message
        /// </summary>
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] SendAIChatMessageDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(ApiResponse<AIChatResponseDto>.FailResponse("Message content is required"));

            if (request.Content.Length > 2000)
                return BadRequest(ApiResponse<AIChatResponseDto>.FailResponse("Message content must be less than 2000 characters"));

            var userId = GetCurrentUserId();
            var response = await _chatService.SendMessageAsync(userId, request);
            return Ok(ApiResponse<AIChatResponseDto>.SuccessResponse(response));
        }

        /// <summary>
        /// Lấy tất cả phiên chat của người dùng hiện tại
        /// GET /api/AIChat/sessions
        /// </summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userId = GetCurrentUserId();
            var sessions = await _chatService.GetSessionsAsync(userId);
            return Ok(ApiResponse<List<AIChatSessionDto>>.SuccessResponse(sessions));
        }

        /// <summary>
        /// Lấy tin nhắn trong một phiên chat cụ thể
        /// GET /api/AIChat/sessions/{sessionId}
        /// </summary>
        [HttpGet("sessions/{sessionId}")]
        public async Task<IActionResult> GetSessionHistory(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(ApiResponse<AIChatHistoryDto>.FailResponse("Session ID is required"));

            var userId = GetCurrentUserId();
            var history = await _chatService.GetSessionHistoryAsync(sessionId, userId);
            return Ok(ApiResponse<AIChatHistoryDto>.SuccessResponse(history));
        }

        /// <summary>
        /// Xóa một phiên chat
        /// DELETE /api/AIChat/sessions/{sessionId}
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(ApiResponse.FailResponse("Session ID is required"));

            var userId = GetCurrentUserId();
            await _chatService.DeleteSessionAsync(sessionId, userId);
            return Ok(ApiResponse.SuccessResponse("Session deleted successfully"));
        }
    }
}
