using MentorEdu.DTOs.Category;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Hashtag;
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
    }
}
