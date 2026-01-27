using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Review;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class MentorController : ControllerBase
    {
        private readonly IMentorService _mentorService;

        public MentorController(IMentorService mentorService)
        {
            _mentorService = mentorService;
        }

        /// <summary>
        /// Search mentors with filters
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResponse<MentorListDto>>>> SearchMentors([FromQuery] SearchMentorRequest request)
        {
            var result = await _mentorService.SearchMentorsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<MentorListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<MentorListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get mentor detail by ID
        /// </summary>
        [HttpGet("{mentorId}")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetMentorDetail(long mentorId)
        {
            var result = await _mentorService.GetMentorDetailAsync(mentorId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<UserDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<UserDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get top rated mentors
        /// </summary>
        [HttpGet("top")]
        public async Task<ActionResult<ApiResponse<List<MentorListDto>>>> GetTopMentors([FromQuery] int count = 10)
        {
            var result = await _mentorService.GetTopMentorsAsync(count);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<MentorListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<MentorListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get mentors by category
        /// </summary>
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<ApiResponse<List<MentorListDto>>>> GetMentorsByCategory(long categoryId)
        {
            var result = await _mentorService.GetMentorsByCategoryAsync(categoryId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<MentorListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<MentorListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get mentor reviews
        /// </summary>
        [HttpGet("{mentorId}/reviews")]
        public async Task<ActionResult<ApiResponse<PagedResponse<MentorReviewDto>>>> GetMentorReviews(
            long mentorId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _mentorService.GetMentorReviewsAsync(mentorId, pageNumber, pageSize);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<MentorReviewDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<MentorReviewDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get mentor review summary
        /// </summary>
        [HttpGet("{mentorId}/reviews/summary")]
        public async Task<ActionResult<ApiResponse<ReviewSummaryDto>>> GetMentorReviewSummary(long mentorId)
        {
            var result = await _mentorService.GetMentorReviewSummaryAsync(mentorId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ReviewSummaryDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<ReviewSummaryDto>.SuccessResponse(result.Data!));
        }

        #region Mentor Application Flow

        /// <summary>
        /// Apply to become a mentor (Student only)
        /// </summary>
        [HttpPost("apply")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ApiResponse>> Apply([FromBody] ApplyAsMentorRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse.FailResponse("Invalid user"));

            var result = await _mentorService.ApplyAsMentorAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return StatusCode(result.StatusCode, ApiResponse.SuccessResponse(result.Message ?? "Application submitted"));
        }

        /// <summary>
        /// Get current mentor application status
        /// </summary>
        [HttpGet("application-status")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<MentorApplicationStatusDto>>> GetApplicationStatus()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse<MentorApplicationStatusDto>.FailResponse("Invalid user"));

            var result = await _mentorService.GetApplicationStatusAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<MentorApplicationStatusDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<MentorApplicationStatusDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Confirm switching role from Student to Mentor (after admin approval)
        /// </summary>
        [HttpPost("confirm-switch")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ApiResponse>> ConfirmSwitch()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse.FailResponse("Invalid user"));

            var result = await _mentorService.ConfirmSwitchToMentorAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message ?? "Role switched successfully"));
        }

        #endregion
    }
}
