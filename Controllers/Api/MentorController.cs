using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Review;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
    }
}
