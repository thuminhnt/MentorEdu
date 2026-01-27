using MentorEdu.DTOs.Admin;
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
            [FromQuery] bool? isActive = null,
            [FromQuery] string? keyword = null)
        {
            var result = await _adminService.GetUsersAsync(pageNumber, pageSize, role, isActive, keyword);

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
        /// Create new user (admin only)
        /// </summary>
        [HttpPost("users")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> CreateUser([FromBody] AdminCreateUserDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDetailDto>.FailResponse("Invalid request data"));

            var result = await _adminService.AdminCreateUserAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<UserDetailDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<UserDetailDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Update user (admin only)
        /// </summary>
        [HttpPut("users/{userId}")]
        public async Task<ActionResult<ApiResponse<UserDetailDto>>> UpdateUser(long userId, [FromBody] AdminUpdateUserDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<UserDetailDto>.FailResponse("Invalid request data"));

            var result = await _adminService.AdminUpdateUserAsync(userId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<UserDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<UserDetailDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Delete user (admin only)
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<ActionResult<ApiResponse>> DeleteUser(long userId)
        {
            var result = await _adminService.AdminDeleteUserAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
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

        #region Category-Hashtag Mapping

        /// <summary>
        /// Get all category-hashtag mappings
        /// </summary>
        [HttpGet("category-hashtags")]
        public async Task<ActionResult<ApiResponse<List<CategoryHashtagMappingDto>>>> GetCategoryHashtagMappings()
        {
            var result = await _adminService.GetCategoryHashtagMappingsAsync();

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<List<CategoryHashtagMappingDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<List<CategoryHashtagMappingDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get hashtags mapped to a specific category
        /// </summary>
        [HttpGet("categories/{categoryId}/hashtags")]
        public async Task<ActionResult<ApiResponse<CategoryHashtagDetailDto>>> GetHashtagsByCategory(long categoryId)
        {
            var result = await _adminService.GetHashtagsByCategoryAsync(categoryId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<CategoryHashtagDetailDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<CategoryHashtagDetailDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Add hashtag mapping to a category
        /// </summary>
        [HttpPost("category-hashtags")]
        public async Task<ActionResult<ApiResponse>> AddCategoryHashtagMapping([FromBody] AddCategoryHashtagDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var result = await _adminService.AddCategoryHashtagMappingAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Remove hashtag mapping from a category
        /// </summary>
        [HttpDelete("category-hashtags")]
        public async Task<ActionResult<ApiResponse>> RemoveCategoryHashtagMapping([FromBody] RemoveCategoryHashtagDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var result = await _adminService.RemoveCategoryHashtagMappingAsync(request);

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

        #region Requests Management

        /// <summary>
        /// Get all requests with filters
        /// </summary>
        [HttpGet("requests")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminRequestListDto>>>> GetAllRequests([FromQuery] AdminSearchRequestDto request)
        {
            var result = await _adminService.GetAllRequestsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminRequestListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminRequestListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Delete a request (admin only)
        /// </summary>
        [HttpDelete("requests/{requestId}")]
        public async Task<ActionResult<ApiResponse>> DeleteRequest(long requestId, [FromQuery] string reason = "Violated policies")
        {
            var result = await _adminService.AdminDeleteRequestAsync(requestId, reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Proposals Management

        /// <summary>
        /// Get all proposals with filters
        /// </summary>
        [HttpGet("proposals")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminProposalListDto>>>> GetAllProposals([FromQuery] AdminSearchProposalDto request)
        {
            var result = await _adminService.GetAllProposalsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminProposalListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminProposalListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Delete a proposal (admin only)
        /// </summary>
        [HttpDelete("proposals/{proposalId}")]
        public async Task<ActionResult<ApiResponse>> DeleteProposal(long proposalId, [FromQuery] string reason = "Violated policies")
        {
            var result = await _adminService.AdminDeleteProposalAsync(proposalId, reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Orders Management

        /// <summary>
        /// Get all orders with filters
        /// </summary>
        [HttpGet("orders")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminOrderListDto>>>> GetAllOrders([FromQuery] AdminSearchOrderDto request)
        {
            var result = await _adminService.GetAllOrdersAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminOrderListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminOrderListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get order detail for admin
        /// </summary>
        [HttpGet("orders/{orderId}")]
        public async Task<ActionResult<ApiResponse<AdminOrderListDto>>> GetOrderDetail(long orderId)
        {
            var result = await _adminService.GetOrderDetailForAdminAsync(orderId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<AdminOrderListDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<AdminOrderListDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Update order status (admin intervention)
        /// </summary>
        [HttpPut("orders/{orderId}/status")]
        public async Task<ActionResult<ApiResponse>> UpdateOrderStatus(long orderId, [FromBody] AdminUpdateOrderStatusDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var adminId = GetCurrentUserId();
            var result = await _adminService.AdminUpdateOrderStatusAsync(orderId, adminId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Refund an order (admin only)
        /// </summary>
        [HttpPost("orders/{orderId}/refund")]
        public async Task<ActionResult<ApiResponse>> RefundOrder(long orderId, [FromBody] AdminRefundOrderDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.FailResponse("Invalid request data"));

            var adminId = GetCurrentUserId();
            var result = await _adminService.AdminRefundOrderAsync(orderId, adminId, request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Community Posts Management

        /// <summary>
        /// Get all community posts with filters
        /// </summary>
        [HttpGet("posts")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminPostListDto>>>> GetAllPosts([FromQuery] AdminSearchPostDto request)
        {
            var result = await _adminService.GetAllPostsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminPostListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminPostListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Delete a post (admin only)
        /// </summary>
        [HttpDelete("posts/{postId}")]
        public async Task<ActionResult<ApiResponse>> DeletePost(long postId, [FromQuery] string reason = "Violated policies")
        {
            var result = await _adminService.AdminDeletePostAsync(postId, reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Hide a post (admin only)
        /// </summary>
        [HttpPost("posts/{postId}/hide")]
        public async Task<ActionResult<ApiResponse>> HidePost(long postId, [FromBody] HideContentRequest? request = null)
        {
            var result = await _adminService.AdminHidePostAsync(postId, request?.Reason ?? "Violated policies");

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Community Comments Management

        /// <summary>
        /// Get all comments with filters
        /// </summary>
        [HttpGet("comments")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminCommentListDto>>>> GetAllComments([FromQuery] AdminSearchCommentDto request)
        {
            var result = await _adminService.GetAllCommentsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminCommentListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminCommentListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Delete a comment (admin only)
        /// </summary>
        [HttpDelete("comments/{commentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteComment(long commentId, [FromQuery] string reason = "Violated policies")
        {
            var result = await _adminService.AdminDeleteCommentAsync(commentId, reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Transactions Management

        /// <summary>
        /// Get all transactions with filters
        /// </summary>
        [HttpGet("transactions")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminTransactionListDto>>>> GetAllTransactions([FromQuery] AdminSearchTransactionDto request)
        {
            var result = await _adminService.GetAllTransactionsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminTransactionListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminTransactionListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Verify a transaction (approve or reject pending transactions)
        /// </summary>
        [HttpPost("transactions/{transactionId}/verify")]
        public async Task<ActionResult<ApiResponse>> VerifyTransaction(long transactionId, [FromBody] VerifyTransactionRequest request)
        {
            var adminId = GetCurrentUserId();
            var result = await _adminService.AdminVerifyTransactionAsync(transactionId, adminId, request.Approve, request.Note);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Wallets Management

        /// <summary>
        /// Get all wallets with filters
        /// </summary>
        [HttpGet("wallets")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminWalletListDto>>>> GetAllWallets([FromQuery] AdminSearchWalletDto request)
        {
            var result = await _adminService.GetAllWalletsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminWalletListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminWalletListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get wallet detail for admin
        /// </summary>
        [HttpGet("wallets/{walletId}")]
        public async Task<ActionResult<ApiResponse<AdminWalletListDto>>> GetWalletDetail(long walletId)
        {
            var result = await _adminService.GetWalletDetailForAdminAsync(walletId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<AdminWalletListDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<AdminWalletListDto>.SuccessResponse(result.Data!));
        }

        #endregion

        #region Reviews Management

        /// <summary>
        /// Get all reviews with filters
        /// </summary>
        [HttpGet("reviews")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminReviewListDto>>>> GetAllReviews([FromQuery] AdminSearchReviewDto request)
        {
            var result = await _adminService.GetAllReviewsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminReviewListDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminReviewListDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Delete a review (admin only)
        /// </summary>
        [HttpDelete("reviews/{reviewId}")]
        public async Task<ActionResult<ApiResponse>> DeleteReview(long reviewId, [FromQuery] string reason = "Inappropriate content")
        {
            var result = await _adminService.AdminDeleteReviewAsync(reviewId, reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Hashtags Management

        /// <summary>
        /// Get all hashtags with filters
        /// </summary>
        [HttpGet("hashtags")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminHashtagDto>>>> GetAllHashtags([FromQuery] AdminSearchHashtagDto request)
        {
            var result = await _adminService.GetAllHashtagsAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<AdminHashtagDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<AdminHashtagDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Create a new hashtag
        /// </summary>
        [HttpPost("hashtags")]
        public async Task<ActionResult<ApiResponse<AdminHashtagDto>>> CreateHashtag([FromBody] CreateHashtagDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AdminHashtagDto>.FailResponse("Invalid request data"));

            var result = await _adminService.CreateHashtagAsync(request);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<AdminHashtagDto>.FailResponse(result.Message ?? ""));

            return StatusCode(201, ApiResponse<AdminHashtagDto>.SuccessResponse(result.Data!, result.Message));
        }

        /// <summary>
        /// Delete a hashtag
        /// </summary>
        [HttpDelete("hashtags/{hashtagId}")]
        public async Task<ActionResult<ApiResponse>> DeleteHashtag(long hashtagId)
        {
            var result = await _adminService.DeleteHashtagAsync(hashtagId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Mentor Applications Management

        /// <summary>
        /// Get all mentor applications with filters
        /// </summary>
        [HttpGet("mentor-applications")]
        public async Task<ActionResult<ApiResponse<PagedResponse<MentorApplicationDto>>>> GetMentorApplications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] MentorApprovalStatus? status = null)
        {
            var result = await _adminService.GetMentorApplicationsAsync(pageNumber, pageSize, status);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<PagedResponse<MentorApplicationDto>>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<PagedResponse<MentorApplicationDto>>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Get mentor application detail by user ID
        /// </summary>
        [HttpGet("mentor-applications/{userId}")]
        public async Task<ActionResult<ApiResponse<MentorApplicationDto>>> GetMentorApplicationDetail(long userId)
        {
            var result = await _adminService.GetMentorApplicationDetailAsync(userId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse<MentorApplicationDto>.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse<MentorApplicationDto>.SuccessResponse(result.Data!));
        }

        /// <summary>
        /// Approve a mentor application - changes user role to Mentor
        /// </summary>
        [HttpPost("mentor-applications/{userId}/approve")]
        public async Task<ActionResult<ApiResponse>> ApproveMentorApplication(long userId)
        {
            var adminId = GetCurrentUserId();
            var result = await _adminService.ApproveMentorApplicationAsync(userId, adminId);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        /// <summary>
        /// Reject a mentor application
        /// </summary>
        [HttpPost("mentor-applications/{userId}/reject")]
        public async Task<ActionResult<ApiResponse>> RejectMentorApplication(long userId, [FromBody] RejectMentorRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(ApiResponse.FailResponse("Rejection reason is required"));

            var adminId = GetCurrentUserId();
            var result = await _adminService.RejectMentorApplicationAsync(userId, adminId, request.Reason);

            if (!result.Success)
                return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

            return Ok(ApiResponse.SuccessResponse(result.Message));
        }

        #endregion

        #region Helper Methods

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        #endregion
    }

    #region Request DTOs

    public class BanUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class HandleReportRequest
    {
        public string Status { get; set; } = string.Empty;
        public bool BanUser { get; set; } = false;
    }

    public class HideContentRequest
    {
        public string? Reason { get; set; }
    }

    public class VerifyTransactionRequest
    {
        public bool Approve { get; set; }
        public string? Note { get; set; }
    }

    #endregion
}
