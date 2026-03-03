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
        /// Lấy danh sách cuộc hội thoại của người dùng hiện tại
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
        /// Lấy chi tiết cuộc hội thoại và tin nhắn
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
        /// Tạo hoặc lấy cuộc hội thoại với người dùng khác
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
        /// Lấy tin nhắn của cuộc hội thoại (có phân trang)
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
        /// Gửi tin nhắn (thay thế cho SignalR)
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
        /// Đánh dấu tin nhắn đã đọc
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
        /// Lấy số lượng tin nhắn chưa đọc
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
