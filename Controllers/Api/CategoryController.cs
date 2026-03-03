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
        private readonly IAdminCategoryService _categoryService;

        public CategoryController(IAdminCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Lấy tất cả danh mục (công khai)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryResponseDto>>>> GetCategories()
        {
            var result = await _categoryService.GetCategoriesAsync();

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CategoryResponseDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CategoryResponseDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Lấy danh mục theo ID (công khai)
        /// </summary>
        [HttpGet("{categoryId}")]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> GetCategoryById(long categoryId)
        {
            var result = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CategoryResponseDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CategoryResponseDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Lấy hashtag theo ID danh mục (công khai)
        /// </summary>
        [HttpGet("{categoryId}/hashtags")]
        public async Task<ActionResult<ApiResponse<CategoryHashtagDetailDto>>> GetHashtagsByCategory(long categoryId)
        {
            var result = await _categoryService.GetHashtagsByCategoryAsync(categoryId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CategoryHashtagDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CategoryHashtagDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Tìm kiếm hashtag (công khai) - dùng cho gợi ý tự động
        /// </summary>
        [HttpGet("/api/hashtags")]
        public async Task<ActionResult<ApiResponse<List<HashtagDto>>>> SearchHashtags([FromQuery] string? keyword = null)
        {
            var result = await _categoryService.GetHashtagsAsync();

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<HashtagDto>>.FailResponse(result.Message ?? ""));

            var hashtags = result.Data ?? new List<HashtagDto>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                hashtags = hashtags
                    .Where(h => h.Name != null && h.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Ok(ApiResponse<List<HashtagDto>>.SuccessResponse(hashtags));
        }
    }
}
