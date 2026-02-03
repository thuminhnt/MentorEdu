using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetProfile()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.GetCurrentUserProfileAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<UserDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<UserDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{userId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUserById(long userId)
        {
            var result = await _userService.GetUserByIdAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<UserDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<UserDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> UpdateProfile([FromBody] UpdateUserProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDetailDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _userService.UpdateUserProfileAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<UserDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<UserDetailDto>.SuccessResponse(result.Data!, "Profile updated successfully"));
        }

        /// <summary>
        /// Update mentor profile (for mentors only)
        /// </summary>
        [HttpPut("mentor-profile")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> UpdateMentorProfile([FromBody] UpdateMentorProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDetailDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _userService.UpdateMentorProfileAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<UserDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<UserDetailDto>.SuccessResponse(result.Data!, "Mentor profile updated successfully"));
        }

        /// <summary>
        /// Update avatar
        /// </summary>
        [HttpPut("avatar")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateAvatar([FromBody] UpdateAvatarRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _userService.UpdateAvatarAsync(userId, request.AvatarUrl);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<string>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<string>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Deactivate current user account
        /// </summary>
        [HttpPost("deactivate")]
        public async Task<ActionResult<ApiResponse>> DeactivateAccount()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeactivateAccountAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Delete current user account
        /// </summary>
        [HttpDelete("account")]
        public async Task<ActionResult<ApiResponse>> DeleteAccount()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.DeleteAccountAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Apply to become a mentor (from profile page)
        /// Requires: Specialization, ExperienceYears, Introduction, PortfolioUrl, CertificationUrls
        /// </summary>
        [HttpPost("apply-mentor")]
        public async Task<ActionResult<ApiResponse>> ApplyAsMentor([FromBody] ApplyAsMentorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _userService.ApplyAsMentorAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Get mentor application status for current user
        /// </summary>
        [HttpGet("mentor-application-status")]
        public async Task<ActionResult<ApiResponse<MentorApplicationStatusDto>>> GetMentorApplicationStatus()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.GetMentorApplicationStatusAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<MentorApplicationStatusDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<MentorApplicationStatusDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Create a report (report a user or review)
        /// </summary>
        [HttpPost("reports")]
        public async Task<ActionResult<ApiResponse<ReportDto>>> CreateReport([FromBody] CreateReportDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<ReportDto>.FailResponse("Invalid request data"));

            var userId = GetCurrentUserId();
            var result = await _userService.CreateReportAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ReportDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<ReportDto>.SuccessResponse(result.Data!, result.Message));
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    public class UpdateAvatarRequest
    {
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
