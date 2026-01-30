using MentorEdu.DTOs.Category;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Hashtag;
using MentorEdu.DTOs.User;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public CategoryController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// Get all categories (public)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryResponseDto>>>> GetCategories()
        {
            var result = await _adminService.GetCategoriesAsync();

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CategoryResponseDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CategoryResponseDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get category by ID (public)
        /// </summary>
        [HttpGet("{categoryId}")]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> GetCategoryById(long categoryId)
        {
            var result = await _adminService.GetCategoryByIdAsync(categoryId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CategoryResponseDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CategoryResponseDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get hashtags by category ID (public)
        /// </summary>
        [HttpGet("{categoryId}/hashtags")]
        public async Task<ActionResult<ApiResponse<CategoryHashtagDetailDto>>> GetHashtagsByCategory(long categoryId)
        {
            var result = await _adminService.GetHashtagsByCategoryAsync(categoryId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CategoryHashtagDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CategoryHashtagDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Search hashtags (public) - for autocomplete
        /// </summary>
        [HttpGet("/api/hashtags")]
        public async Task<ActionResult<ApiResponse<List<HashtagDto>>>> SearchHashtags([FromQuery] string? keyword = null)
        {
            var result = await _adminService.GetHashtagsAsync();

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<HashtagDto>>.FailResponse(result.Message ?? ""));

            var hashtags = result.Data ?? new List<HashtagDto>();
            
            // Filter by keyword if provided
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                hashtags = hashtags
                    .Where(h => h.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Ok(ApiResponse<List<HashtagDto>>.SuccessResponse(hashtags));
        }
    }
}
