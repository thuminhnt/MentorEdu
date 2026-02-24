using MentorEdu.DTOs.AIChat;
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
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(IAIChatService chatService, ILogger<AIChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user ID from claims
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
        /// Send a message to AI chatbot
        /// POST /api/AIChat/message
        /// </summary>
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] SendAIChatMessageDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Message content is required" });
            }

            if (request.Content.Length > 2000)
            {
                return BadRequest(new { error = "Message content must be less than 2000 characters" });
            }

            try
            {
                var userId = GetCurrentUserId();
                var response = await _chatService.SendMessageAsync(userId, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to AI");
                return StatusCode(500, new { error = "Failed to process message. Please try again." });
            }
        }

        /// <summary>
        /// Get all chat sessions for current user
        /// GET /api/AIChat/sessions
        /// </summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            try
            {
                var userId = GetCurrentUserId();
                var sessions = await _chatService.GetSessionsAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat sessions");
                return StatusCode(500, new { error = "Failed to get sessions" });
            }
        }

        /// <summary>
        /// Get messages in a specific session
        /// GET /api/AIChat/sessions/{sessionId}
        /// </summary>
        [HttpGet("sessions/{sessionId}")]
        public async Task<IActionResult> GetSessionHistory(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            try
            {
                var userId = GetCurrentUserId();
                var history = await _chatService.GetSessionHistoryAsync(sessionId, userId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session history");
                return StatusCode(500, new { error = "Failed to get session history" });
            }
        }

        /// <summary>
        /// Delete a chat session
        /// DELETE /api/AIChat/sessions/{sessionId}
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            try
            {
                var userId = GetCurrentUserId();
                await _chatService.DeleteSessionAsync(sessionId, userId);
                return Ok(new { message = "Session deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session");
                return StatusCode(500, new { error = "Failed to delete session" });
            }
        }
    }
}
