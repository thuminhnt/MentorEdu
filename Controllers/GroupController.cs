using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Groups;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ILogger<GroupController> _logger;

        public GroupController(IGroupService groupService, ILogger<GroupController> logger)
        {
            _groupService = groupService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new group (Mentor only)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _groupService.CreateGroupAsync(userId, dto);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<GroupDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<GroupDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Join a group using invite code
        /// </summary>
        [HttpPost("join/{inviteCode}")]
        public async Task<IActionResult> JoinGroup(string inviteCode)
        {
            var userId = GetCurrentUserId();
            var result = await _groupService.JoinGroupAsync(userId, inviteCode);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<GroupDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<GroupDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get group details by ID
        /// </summary>
        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroup(long groupId)
        {
            var result = await _groupService.GetGroupByIdAsync(groupId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<GroupDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<GroupDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get all groups for current user
        /// </summary>
        [HttpGet("my-groups")]
        public async Task<IActionResult> GetMyGroups()
        {
            var userId = GetCurrentUserId();
            var result = await _groupService.GetGroupsForUserAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<GroupDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<GroupDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get messages for a group
        /// </summary>
        [HttpGet("{groupId}/messages")]
        public async Task<IActionResult> GetGroupMessages(long groupId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            var result = await _groupService.GetGroupMessagesAsync(groupId, userId, pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<object>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<object>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Leave a group
        /// </summary>
        [HttpPost("{groupId}/leave")]
        public async Task<IActionResult> LeaveGroup(long groupId)
        {
            var userId = GetCurrentUserId();
            var result = await _groupService.LeaveGroupAsync(userId, groupId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<bool>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<bool>.SuccessResponse(result.Data));
        }

        /// <summary>
        /// Add member to group (Admin only)
        /// </summary>
        [HttpPost("{groupId}/members/{userId}")]
        public async Task<IActionResult> AddMember(long groupId, long userId)
        {
            var adminId = GetCurrentUserId();
            var result = await _groupService.AddMemberAsync(adminId, groupId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<GroupDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<GroupDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Send message to group
        /// </summary>
        [HttpPost("{groupId}/messages")]
        public async Task<IActionResult> SendMessage(long groupId, [FromBody] SendGroupMessageDto dto)
        {
            var userId = GetCurrentUserId();
            dto.GroupId = groupId;
            var result = await _groupService.SendGroupMessageAsync(userId, dto);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<GroupMessageDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<GroupMessageDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Remove (kick) a member from group (Admin only)
        /// </summary>
        [HttpPost("{groupId}/members/{userId}/kick")]
        public async Task<IActionResult> RemoveMember(long groupId, long userId)
        {
            var adminId = GetCurrentUserId();
            var result = await _groupService.RemoveMemberAsync(adminId, groupId, userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<bool>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<bool>.SuccessResponse(result.Data));
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
