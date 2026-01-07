using System.Security.Claims;
using MentorEdu.DTOs.Chat;
using MentorEdu.DTOs.Common;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Get current user's conversations
        /// </summary>
        [HttpGet("conversations")]
        public async Task<ActionResult<ApiResponse<List<ConversationListDto>>>> GetConversations()
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.GetUserConversationsAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<ConversationListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<ConversationListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get conversation detail and messages
        /// </summary>
        [HttpGet("conversations/{conversationId}")]
        public async Task<ActionResult<ApiResponse<ConversationDetailDto>>> GetConversation(long conversationId)
        {
            var userId = GetCurrentUserId();
            
            // Check access
            var hasAccess = await _chatService.UserHasAccessToConversationAsync(userId, conversationId);
            if (!hasAccess)
                return Forbid();

            var result = await _chatService.GetConversationByIdAsync(conversationId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ConversationDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<ConversationDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Create or get conversation with another user
        /// </summary>
        [HttpPost("conversations")]
        public async Task<ActionResult<ApiResponse<ConversationDetailDto>>> CreateOrGetConversation([FromBody] CreateConversationDto request)
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.GetOrCreateConversationAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ConversationDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<ConversationDetailDto>.SuccessResponse(result.Data!, "Conversation created successfully"));
        }

        /// <summary>
        /// Get messages of a conversation (paginated)
        /// </summary>
        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<ActionResult<ApiResponse<PagedResponse<MessageDto>>>> GetMessages(
            long conversationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            
            var result = await _chatService.GetMessagesAsync(userId, new GetMessagesDto
            {
                ConversationId = conversationId,
                PageNumber = pageNumber,
                PageSize = pageSize
            });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<MessageDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<MessageDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Send a message (alternative to SignalR)
        /// </summary>
        [HttpPost("conversations/{conversationId}/messages")]
        public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage(
            long conversationId,
            [FromBody] SendMessageRequest request)
        {
            var userId = GetCurrentUserId();

            var result = await _chatService.SendMessageAsync(userId, new SendMessageDto
            {
                ConversationId = conversationId,
                Content = request.Content,
                MessageType = request.MessageType
            });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<MessageDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<MessageDto>.SuccessResponse(result.Data!, "Message sent successfully"));
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        [HttpPost("conversations/{conversationId}/read")]
        public async Task<ActionResult<ApiResponse>> MarkAsRead(long conversationId)
        {
            var userId = GetCurrentUserId();
            
            var hasAccess = await _chatService.UserHasAccessToConversationAsync(userId, conversationId);
            if (!hasAccess)
                return Forbid();

            await _chatService.MarkMessagesAsReadAsync(conversationId, userId);

            return Ok(ApiResponse.SuccessResponse("Marked as read successfully"));
        }

        /// <summary>
        /// Get unread message count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var result = await _chatService.GetUnreadCountAsync(userId);

            return Ok(ApiResponse<int>.SuccessResponse(result.Data));
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    public class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public MentorEdu.Models.Enums.MessageType MessageType { get; set; } = MentorEdu.Models.Enums.MessageType.Text;
    }
}
