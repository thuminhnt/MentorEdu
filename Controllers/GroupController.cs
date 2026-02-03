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
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupService.CreateGroupAsync(userId, dto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Join a group using invite code
        /// </summary>
        [HttpPost("join/{inviteCode}")]
        public async Task<IActionResult> JoinGroup(string inviteCode)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupService.JoinGroupAsync(userId, inviteCode);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get group details by ID
        /// </summary>
        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroup(long groupId)
        {
            var result = await _groupService.GetGroupByIdAsync(groupId);
            if (!result.Success)
            {
                return NotFound(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get all groups for current user
        /// </summary>
        [HttpGet("my-groups")]
        public async Task<IActionResult> GetMyGroups()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupService.GetGroupsForUserAsync(userId);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get messages for a group
        /// </summary>
        [HttpGet("{groupId}/messages")]
        public async Task<IActionResult> GetGroupMessages(long groupId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupService.GetGroupMessagesAsync(groupId, userId, pageNumber, pageSize);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        /// <summary>
        /// Leave a group
        /// </summary>
        [HttpPost("{groupId}/leave")]
        public async Task<IActionResult> LeaveGroup(long groupId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _groupService.LeaveGroupAsync(userId, groupId);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
