using MentorEdu.DTOs.Category;
using MentorEdu.DTOs.Common;
using MentorEdu.DTOs.Report;
using MentorEdu.DTOs.User;
using MentorEdu.Models.Enums;
using MentorEdu.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        #region Dashboard

        /// <summary>
        /// Get admin dashboard data
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard()
        {
            var result = await _adminService.GetDashboardAsync();

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<AdminDashboardDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(result.Data!));
        }

        #endregion

        #region User Management

        /// <summary>
        /// Get all users with filters
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<PagedResponse<UserBasicDto>>>> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] UserRole? role = null,
            [FromQuery] bool? isActive = null)
        {
            var result = await _adminService.GetUsersAsync(pageNumber, pageSize, role, isActive);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<UserBasicDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<UserBasicDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get user detail by ID
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUserDetail(long userId)
        {
            var result = await _adminService.GetUserDetailAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<UserDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<UserDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Activate user account
        /// </summary>
        [HttpPost("users/{userId}/activate")]
        public async Task<ActionResult<ApiResponse>> ActivateUser(long userId)
        {
            var result = await _adminService.ActivateUserAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Deactivate user account
        /// </summary>
        [HttpPost("users/{userId}/deactivate")]
        public async Task<ActionResult<ApiResponse>> DeactivateUser(long userId)
        {
            var result = await _adminService.DeactivateUserAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Ban user account
        /// </summary>
        [HttpPost("users/{userId}/ban")]
        public async Task<ActionResult<ApiResponse>> BanUser(long userId, [FromBody] BanUserRequest request)
        {
            var result = await _adminService.BanUserAsync(userId, request.Reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Verify user account
        /// </summary>
        [HttpPost("users/{userId}/verify")]
        public async Task<ActionResult<ApiResponse>> VerifyUser(long userId)
        {
            var result = await _adminService.VerifyUserAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Category Management

        /// <summary>
        /// Get all categories
        /// </summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<CategoryResponseDto>>>> GetCategories()
        {
            var result = await _adminService.GetCategoriesAsync();

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CategoryResponseDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CategoryResponseDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("categories/{categoryId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> GetCategoryById(long categoryId)
        {
            var result = await _adminService.GetCategoryByIdAsync(categoryId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CategoryResponseDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CategoryResponseDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost("categories")]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CategoryResponseDto>.FailResponse("Invalid request data"));

            var result = await _adminService.CreateCategoryAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CategoryResponseDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<CategoryResponseDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Update a category
        /// </summary>
        [HttpPut("categories/{categoryId}")]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> UpdateCategory(long categoryId, [FromBody] UpdateCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CategoryResponseDto>.FailResponse("Invalid request data"));

            var result = await _adminService.UpdateCategoryAsync(categoryId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CategoryResponseDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CategoryResponseDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Delete a category
        /// </summary>
        [HttpDelete("categories/{categoryId}")]
        public async Task<ActionResult<ApiResponse>> DeleteCategory(long categoryId)
        {
            var result = await _adminService.DeleteCategoryAsync(categoryId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Report Management

        /// <summary>
        /// Get all reports with filters
        /// </summary>
        [HttpGet("reports")]
        public async Task<ActionResult<ApiResponse<PagedResponse<ReportDto>>>> GetReports(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var result = await _adminService.GetReportsAsync(pageNumber, pageSize, status);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<ReportDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<ReportDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get report detail by ID
        /// </summary>
        [HttpGet("reports/{reportId}")]
        public async Task<ActionResult<ApiResponse<ReportDetailDto>>> GetReportDetail(long reportId)
        {
            var result = await _adminService.GetReportDetailAsync(reportId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<ReportDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<ReportDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Handle a report
        /// </summary>
        [HttpPost("reports/{reportId}/handle")]
        public async Task<ActionResult<ApiResponse>> HandleReport(long reportId, [FromBody] HandleReportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var adminId = GetCurrentUserId();
            var result = await _adminService.HandleReportAsync(adminId, new HandleReportDto
            {
                ReportId = reportId,
                Status = request.Status,
                BanUser = request.BanUser
            });

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    public class BanUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class HandleReportRequest
    {
        public string Status { get; set; } = string.Empty;
        public bool BanUser { get; set; } = false;
    }
}
